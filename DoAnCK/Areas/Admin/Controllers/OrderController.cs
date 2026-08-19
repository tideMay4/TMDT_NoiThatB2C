using DoAnCK.Areas.Admin.Models;
using DoAnCK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnCK.Areas.Admin.Controllers
{
    public class OrderController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        private readonly List<string> OrderStatuses = new List<string>
        {
            "Chờ xác nhận",
            "Đã xác nhận",
            "Đang giao",
            "Hoàn thành",
            "Đã hủy"
        };

        public ActionResult Index(string keyword, string status, string fromDate, string toDate)
        {
            List<AdminOrderListItemViewModel> allOrders = GetAllOrders();

            var query = allOrders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();

                query = query.Where(x =>
                    x.MaDH.ToString().Contains(keyword) ||
                    (!string.IsNullOrEmpty(x.TenKhachHang) && x.TenKhachHang.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(x.Email) && x.Email.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(x.SDT) && x.SDT.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrEmpty(x.SanPhamTomTat) && x.SanPhamTomTat.ToLower().Contains(keyword))
                );
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.TrangThai == status);
            }

            DateTime from;

            if (DateTime.TryParse(fromDate, out from))
            {
                query = query.Where(x => x.NgayDat.HasValue && x.NgayDat.Value.Date >= from.Date);
            }

            DateTime to;

            if (DateTime.TryParse(toDate, out to))
            {
                query = query.Where(x => x.NgayDat.HasValue && x.NgayDat.Value.Date <= to.Date);
            }

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Statuses = OrderStatuses;

            ViewBag.TotalOrders = allOrders.Count;
            ViewBag.PendingOrders = allOrders.Count(x => x.TrangThai == "Chờ xác nhận");
            ViewBag.ConfirmedOrders = allOrders.Count(x => x.TrangThai == "Đã xác nhận");
            ViewBag.ShippingOrders = allOrders.Count(x => x.TrangThai == "Đang giao");
            ViewBag.CompletedOrders = allOrders.Count(x => x.TrangThai == "Hoàn thành");
            ViewBag.CancelledOrders = allOrders.Count(x => x.TrangThai == "Đã hủy");

            var model = query
                .OrderByDescending(x => x.NgayDat)
                .ThenByDescending(x => x.MaDH)
                .ToList();

            return View(model);
        }

        public ActionResult Details(int id)
        {
            AdminOrderDetailViewModel order = GetOrderDetail(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            ViewBag.Statuses = OrderStatuses;

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string status, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(status) || !OrderStatuses.Contains(status))
            {
                TempData["Error"] = "Trạng thái đơn hàng không hợp lệ.";

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index");
            }

            var order = db.DONHANGs.Find(id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng.";

                return RedirectToAction("Index");
            }

            order.TrangThai = status;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Details", new { id = id });
        }

        private List<AdminOrderListItemViewModel> GetAllOrders()
        {
            string sql = @"
                SELECT
                    DH.MaDH,
                    DH.MaKH,
                    DH.NgayDat,
                    ISNULL(TK.HoTen, KH.HoTen) AS TenKhachHang,
                    ISNULL(TK.Email, '') AS Email,
                    ISNULL(KH.SDT, TK.SDT) AS SDT,
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
                INNER JOIN KHACHHANG KH ON DH.MaKH = KH.MaKH
                LEFT JOIN TAIKHOAN TK ON KH.MaTK = TK.MaTK
            ";

            var orders = db.Database.SqlQuery<AdminOrderListItemViewModel>(sql).ToList();

            foreach (var item in orders)
            {
                if (string.IsNullOrWhiteSpace(item.TrangThai))
                {
                    item.TrangThai = "Chờ xác nhận";
                }

                if (string.IsNullOrWhiteSpace(item.SanPhamTomTat))
                {
                    item.SanPhamTomTat = "Chưa có sản phẩm";
                }

                if (string.IsNullOrWhiteSpace(item.TenKhachHang))
                {
                    item.TenKhachHang = "Khách hàng";
                }
            }

            return orders;
        }

        private AdminOrderDetailViewModel GetOrderDetail(int id)
        {
            string orderSql = @"
                SELECT TOP 1
                    DH.MaDH,
                    DH.MaKH,
                    DH.NgayDat,
                    ISNULL(TK.HoTen, KH.HoTen) AS TenKhachHang,
                    ISNULL(TK.Email, '') AS Email,
                    ISNULL(KH.SDT, TK.SDT) AS SDT,
                    DH.DiaChiGiaoHang,
                    DH.GhiChu,
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
                INNER JOIN KHACHHANG KH ON DH.MaKH = KH.MaKH
                LEFT JOIN TAIKHOAN TK ON KH.MaTK = TK.MaTK
                WHERE DH.MaDH = @p0
            ";

            var order = db.Database.SqlQuery<AdminOrderDetailViewModel>(orderSql, id).FirstOrDefault();

            if (order == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(order.TrangThai))
            {
                order.TrangThai = "Chờ xác nhận";
            }

            string itemSql = @"
                SELECT
                    CT.MaSP,
                    SP.TenSP,
                    SP.HinhAnh,
                    CT.SoLuong,
                    CT.GiaBan,
                    CT.ThanhTien
                FROM CHITIET_DONHANG CT
                INNER JOIN SANPHAM SP ON CT.MaSP = SP.MaSP
                WHERE CT.MaDH = @p0
                ORDER BY CT.MaSP
            ";

            order.Items = db.Database.SqlQuery<AdminOrderDetailItemViewModel>(itemSql, id).ToList();

            return order;
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