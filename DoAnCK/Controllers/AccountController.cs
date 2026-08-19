using DoAnCK.Helpers;
using DoAnCK.Models;
using DoAnCK.Models.ViewModel;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAnCK.Controllers
{
    public class AccountController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        [HttpGet]
        public ActionResult Test()
        {
            return Content("AccountController da chay duoc");
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email.Trim();
            string password = model.Password.Trim();

            var user = db.TAIKHOANs.FirstOrDefault(u =>
                u.Email == email &&
                u.MatKhau == password &&
                u.TrangThai == true
            );

            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa.");
                return View(model);
            }

            string accessToken = JwtHelper.GenerateAccessToken(user.MaTK, user.Email, user.VaiTro);
            string refreshToken = JwtHelper.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7);
            db.SaveChanges();

            SetJwtCookie("accessToken", accessToken, DateTime.Now.AddMinutes(30));
            SetJwtCookie("refreshToken", refreshToken, DateTime.Now.AddDays(7));

            // Lưu thông tin tài khoản vào Session
            Session["MaTK"] = user.MaTK;
            Session["HoTen"] = user.HoTen;
            Session["Email"] = user.Email;
            Session["VaiTro"] = user.VaiTro;

            // Nếu là Customer thì lấy MaKH để Checkout lưu đơn hàng
            if (user.VaiTro == "Customer")
            {
                var khachHang = db.KHACHHANGs.FirstOrDefault(x => x.MaTK == user.MaTK);

                if (khachHang != null)
                {
                    Session["MaKH"] = khachHang.MaKH;
                    Session["TenKhachHang"] = khachHang.HoTen;
                    Session["SDT"] = khachHang.SDT;
                    Session["DiaChi"] = khachHang.DiaChi;
                }
            }

            // Nếu là Store thì lấy MaCH/MaTKStore để StoreDashboard lọc đúng dữ liệu
            if (user.VaiTro == "Store")
            {
                var cuaHang = db.CUAHANGs.FirstOrDefault(x => x.MaTK == user.MaTK);

                if (cuaHang != null)
                {
                    Session["MaCH"] = cuaHang.MaTK;
                    Session["MaTKStore"] = cuaHang.MaTK;
                    Session["TenCuaHang"] = cuaHang.TenCH;
                }
            }

            switch (user.VaiTro)
            {
                case "Admin":
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                case "Store":
                    return RedirectToAction("Index", "StoreDashboard", new { area = "Admin" });

                case "Customer":
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        private void SetJwtCookie(string name, string value, DateTime expires)
        {
            HttpCookie cookie = new HttpCookie(name, value)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Expires = expires,
                Path = "/"
            };

            Response.Cookies.Add(cookie);
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email.Trim();

            bool emailDaTonTai = db.TAIKHOANs.Any(u => u.Email.ToLower() == email.ToLower());

            if (emailDaTonTai)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng chọn Email khác.");
                return View(model);
            }

            TAIKHOAN tk = new TAIKHOAN
            {
                HoTen = model.HoTen.Trim(),
                Email = email,
                MatKhau = model.MatKhau.Trim(),
                SDT = model.SDT,
                VaiTro = "Customer",
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            db.TAIKHOANs.Add(tk);
            db.SaveChanges();

            KHACHHANG kh = new KHACHHANG
            {
                MaTK = tk.MaTK,
                HoTen = model.HoTen.Trim(),
                SDT = model.SDT,
                DiaChi = model.DiaChi,
                NgayDangKy = DateTime.Now
            };

            db.KHACHHANGs.Add(kh);
            db.SaveChanges();

            TempData["SuccessMsg"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (Request.Cookies["accessToken"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return Content("Chức năng quên mật khẩu đang được phát triển.");
        }

        [HttpGet]
        public ActionResult MyAccount()
        {
            if (Session["MaTK"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int maTK = Convert.ToInt32(Session["MaTK"]);

            string sql = @"
        SELECT TOP 1
            TK.MaTK,
            KH.MaKH,
            ISNULL(TK.HoTen, KH.HoTen) AS HoTen,
            TK.Email,
            ISNULL(KH.SDT, TK.SDT) AS SDT,
            KH.DiaChi,
            KH.NgayDangKy
        FROM TAIKHOAN TK
        INNER JOIN KHACHHANG KH ON TK.MaTK = KH.MaTK
        WHERE TK.MaTK = @p0
    ";

            var model = db.Database.SqlQuery<CustomerAccountViewModel>(sql, maTK).FirstOrDefault();

            if (model == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin tài khoản.";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult OrderHistory()
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int maKH = Convert.ToInt32(Session["MaKH"]);

            string sql = @"
        SELECT
            DH.MaDH,
            DH.NgayDat,
            DH.DiaChiGiaoHang,
            DH.TongTien,
            DH.TrangThai,
            STUFF
            (
                (
                    SELECT N', ' + SP2.TenSP
                    FROM CHITIET_DONHANG CT2
                    INNER JOIN SANPHAM SP2 ON CT2.MaSP = SP2.MaSP
                    WHERE CT2.MaDH = DH.MaDH
                    FOR XML PATH(''), TYPE
                ).value('.', 'NVARCHAR(MAX)'),
                1,
                2,
                ''
            ) AS SanPhamTomTat
        FROM DONHANG DH
        WHERE DH.MaKH = @p0
        ORDER BY DH.NgayDat DESC, DH.MaDH DESC
    ";

            var model = db.Database.SqlQuery<CustomerOrderHistoryViewModel>(sql, maKH).ToList();

            return View(model);
        }

        [HttpGet]
        public ActionResult MyReviews()
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int maKH = Convert.ToInt32(Session["MaKH"]);

            string sql = @"
        SELECT
            DG.MaDG,
            DG.MaSP,
            SP.TenSP,
            SP.HinhAnh,
            DG.SoSao,
            DG.NoiDung,
            DG.NgayDanhGia
        FROM DANHGIA DG
        INNER JOIN SANPHAM SP ON DG.MaSP = SP.MaSP
        WHERE DG.MaKH = @p0
        ORDER BY DG.NgayDanhGia DESC, DG.MaDG DESC
    ";

            var model = db.Database.SqlQuery<CustomerReviewViewModel>(sql, maKH).ToList();

            return View(model);
        }

        [HttpGet]
        public ActionResult MyFavorites()
        {
            if (Session["MaKH"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int maKH = Convert.ToInt32(Session["MaKH"]);

            string sql = @"
        SELECT 
            SP.MaSP,
            SP.TenSP,
            SP.HinhAnh,
            SP.GiaHienTai,
            SP.Slug,
            DM.TenDM
        FROM YEUTHICH YT
        INNER JOIN SANPHAM SP ON YT.MaSP = SP.MaSP
        LEFT JOIN DANHMUC DM ON SP.MaDM = DM.MaDM
        WHERE YT.MaKH = @p0
        ORDER BY YT.NgayThem DESC
    ";

            var model = db.Database.SqlQuery<FavoriteProductViewModel>(sql, maKH).ToList();

            return View(model);
        }
        public ActionResult Logout()
        {
            if (Request.Cookies["accessToken"] != null)
            {
                HttpCookie cookie = new HttpCookie("accessToken")
                {
                    Expires = DateTime.Now.AddDays(-1),
                    Path = "/"
                };

                Response.Cookies.Add(cookie);
            }

            if (Request.Cookies["refreshToken"] != null)
            {
                HttpCookie cookie = new HttpCookie("refreshToken")
                {
                    Expires = DateTime.Now.AddDays(-1),
                    Path = "/"
                };

                Response.Cookies.Add(cookie);
            }

            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}