using DoAnCK.Helpers;
using DoAnCK.Models;
using DoAnCK.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAnCK.Controllers
{
    public class AccountController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.TAIKHOANs.FirstOrDefault(u => u.Email == model.Email
                                                         && u.MatKhau == model.Password
                                                         && u.TrangThai == true);

                if (user != null)
                {
                    // 1. Tạo AccessToken và RefreshToken
                    string accessToken = JwtHelper.GenerateAccessToken(user.MaTK, user.Email, user.VaiTro);
                    string refreshToken = JwtHelper.GenerateRefreshToken();

                    // 2. Lưu RefreshToken vào Database
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.Now.AddDays(7); // Sống 7 ngày
                    db.SaveChanges();

                    // 3. Lưu Token vào HttpOnly Cookie (Bảo mật F12 / JavaScript không đọc được)
                    SetJwtCookie("accessToken", accessToken, DateTime.Now.AddMinutes(30));
                    SetJwtCookie("refreshToken", refreshToken, DateTime.Now.AddDays(7));

                    // 4. Điều hướng theo VaiTro
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

                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa.");
            }

            return View(model);
        }

        // Hàm hỗ trợ thiết lập HttpOnly Cookie
        private void SetJwtCookie(string name, string value, DateTime expires)
        {
            var cookie = new HttpCookie(name, value)
            {
                HttpOnly = true,  // CỰC KỲ QUAN TRỌNG: Ngăn chặn JavaScript (F12 Console) truy cập
                Secure = Request.IsSecureConnection, // Bật True nếu dùng HTTPS
                Expires = expires,
                Path = "/"
            };
            Response.Cookies.Add(cookie);
        }

        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Kiểm tra Email đã tồn tại chưa
                var checkEmail = db.TAIKHOANs.FirstOrDefault(u => u.Email.ToLower() == model.Email.ToLower());
                if (checkEmail != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng chọn Email khác.");
                    return View(model);
                }

                // 2. Tạo bản ghi TAIKHOAN (Chứa HoTen, Email, MatKhau, SDT)
                var tk = new TAIKHOAN
                {
                    HoTen = model.HoTen,
                    Email = model.Email,
                    MatKhau = model.MatKhau,
                    SDT = model.SDT,
                    VaiTro = "Customer", // Set role mặc định là Khách hàng
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                db.TAIKHOANs.Add(tk);
                db.SaveChanges(); // Lưu TAIKHOAN để tự sinh MaTK

                // 3. Tạo bản ghi KHACHHANG (Chỉ chứa MaTK, DiaChi, NgayDangKy)
                var kh = new KHACHHANG
                {
                    MaTK = tk.MaTK,
                    DiaChi = model.DiaChi,
                    NgayDangKy = DateTime.Now
                };

                db.KHACHHANGs.Add(kh);
                db.SaveChanges();

                TempData["SuccessMsg"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        public ActionResult Logout()
        {
            // Xóa Cookie phía Browser
            if (Request.Cookies["accessToken"] != null)
            {
                var cookie = new HttpCookie("accessToken") { Expires = DateTime.Now.AddDays(-1), Path = "/" };
                Response.Cookies.Add(cookie);
            }
            if (Request.Cookies["refreshToken"] != null)
            {
                var cookie = new HttpCookie("refreshToken") { Expires = DateTime.Now.AddDays(-1), Path = "/" };
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Login");
        }
    }
}