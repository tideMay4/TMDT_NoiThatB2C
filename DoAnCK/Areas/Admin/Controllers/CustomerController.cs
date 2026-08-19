using DoAnCK.Areas.Admin.Models;
using DoAnCK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnCK.Areas.Admin.Controllers
{
    public class CustomerController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index(string keyword, string status, string sort)
        {
            List<AdminCustomerViewModel> customers = GetCustomers();

            var query = customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string key = keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.MaKH.ToString().Contains(key) ||
                    (x.MaTK.HasValue && x.MaTK.Value.ToString().Contains(key)) ||
                    (!string.IsNullOrEmpty(x.HoTen) && x.HoTen.ToLower().Contains(key)) ||
                    (!string.IsNullOrEmpty(x.Email) && x.Email.ToLower().Contains(key)) ||
                    (!string.IsNullOrEmpty(x.SDT) && x.SDT.ToLower().Contains(key)) ||
                    (!string.IsNullOrEmpty(x.DiaChi) && x.DiaChi.ToLower().Contains(key))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "active")
                {
                    query = query.Where(x => x.TrangThai);
                }
                else if (status == "locked")
                {
                    query = query.Where(x => !x.TrangThai);
                }
                else if (status == "has-order")
                {
                    query = query.Where(x => x.SoDonHang > 0);
                }
                else if (status == "no-order")
                {
                    query = query.Where(x => x.SoDonHang == 0);
                }
            }

            switch (sort)
            {
                case "oldest":
                    query = query.OrderBy(x => x.NgayDangKy);
                    break;

                case "spend-desc":
                    query = query.OrderByDescending(x => x.TongChiTieu);
                    break;

                case "order-desc":
                    query = query.OrderByDescending(x => x.SoDonHang);
                    break;

                case "recent-buy":
                    query = query.OrderByDescending(x => x.LanMuaGanNhat);
                    break;

                default:
                    query = query.OrderByDescending(x => x.NgayDangKy);
                    break;
            }

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.Sort = sort;

            ViewBag.TotalCustomers = customers.Count;
            ViewBag.ActiveCustomers = customers.Count(x => x.TrangThai);
            ViewBag.LockedCustomers = customers.Count(x => !x.TrangThai);
            ViewBag.HasOrderCustomers = customers.Count(x => x.SoDonHang > 0);

            DateTime now = DateTime.Now;

            ViewBag.NewCustomersThisMonth = customers.Count(x =>
                x.NgayDangKy.HasValue &&
                x.NgayDangKy.Value.Month == now.Month &&
                x.NgayDangKy.Value.Year == now.Year
            );

            return View(query.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int MaTK, bool TrangThai, string returnUrl)
        {
            var account = db.TAIKHOANs.Find(MaTK);

            if (account == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản khách hàng.";
                return RedirectToAction("Index");
            }

            if (account.VaiTro != "Customer")
            {
                TempData["Error"] = "Chỉ được cập nhật trạng thái tài khoản khách hàng.";
                return RedirectToAction("Index");
            }

            account.TrangThai = TrangThai;
            db.SaveChanges();

            TempData["Success"] = TrangThai
                ? "Đã mở khóa tài khoản khách hàng."
                : "Đã khóa tài khoản khách hàng.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        private List<AdminCustomerViewModel> GetCustomers()
        {
            string sql = @"
                SELECT
                    KH.MaKH,
                    KH.MaTK,
                    ISNULL(KH.HoTen, TK.HoTen) AS HoTen,
                    ISNULL(TK.Email, '') AS Email,
                    ISNULL(KH.SDT, TK.SDT) AS SDT,
                    ISNULL(KH.DiaChi, '') AS DiaChi,
                    KH.NgayDangKy,
                    TK.NgayTao AS NgayTaoTaiKhoan,
                    ISNULL(TK.TrangThai, 0) AS TrangThai,

                    (
                        SELECT COUNT(*)
                        FROM DONHANG DH
                        WHERE DH.MaKH = KH.MaKH
                    ) AS SoDonHang,

                    (
                        SELECT ISNULL(SUM(DH.TongTien), 0)
                        FROM DONHANG DH
                        WHERE DH.MaKH = KH.MaKH
                          AND ISNULL(DH.TrangThai, N'') <> N'Đã hủy'
                    ) AS TongChiTieu,

                    (
                        SELECT MAX(DH.NgayDat)
                        FROM DONHANG DH
                        WHERE DH.MaKH = KH.MaKH
                    ) AS LanMuaGanNhat

                FROM KHACHHANG KH
                LEFT JOIN TAIKHOAN TK ON KH.MaTK = TK.MaTK
                WHERE TK.VaiTro = 'Customer' OR TK.VaiTro IS NULL
            ";

            var customers = db.Database.SqlQuery<AdminCustomerViewModel>(sql).ToList();

            foreach (var item in customers)
            {
                if (string.IsNullOrWhiteSpace(item.HoTen))
                {
                    item.HoTen = "Khách hàng #" + item.MaKH;
                }

                if (string.IsNullOrWhiteSpace(item.Email))
                {
                    item.Email = "Chưa cập nhật";
                }

                if (string.IsNullOrWhiteSpace(item.SDT))
                {
                    item.SDT = "Chưa cập nhật";
                }

                if (string.IsNullOrWhiteSpace(item.DiaChi))
                {
                    item.DiaChi = "Chưa cập nhật";
                }
            }

            return customers;
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