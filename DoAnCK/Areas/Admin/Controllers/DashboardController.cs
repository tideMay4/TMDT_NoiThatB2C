using DoAnCK.Areas.Admin.Models;
using DoAnCK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnCK.Areas.Admin.Controllers
{
    public class DashboardController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();
        public ActionResult Index()
        {
            return View();
        }

        // Action này chỉ được gọi từ trong một View khác, không thể gọi trực tiếp từ trình duyệt
        [ChildActionOnly]
        //Sản phẩm bán chạy
        public ActionResult _BestSellers()
        {
            var validStatuses = new[] { "Đã xác nhận", "Đang giao", "Hoàn thành" };

            var bestSellers = db.CHITIET_DONHANG
                .Where(ct => validStatuses.Contains(ct.DONHANG.TrangThai))
                .GroupBy(ct => new {
                    ct.MaSP,
                    ct.SANPHAM.TenSP,
                    ct.SANPHAM.HinhAnh,
                    TenDM = ct.SANPHAM.DANHMUC != null ? ct.SANPHAM.DANHMUC.TenDM : "Nội thất"
                })
                .Select(group => new BestSellerViewModel
                {
                    MaSP = group.Key.MaSP,
                    TenSP = group.Key.TenSP,
                    HinhAnh = group.Key.HinhAnh,
                    TenDanhMuc = group.Key.TenDM,
                    GiaBan = group.FirstOrDefault().SANPHAM.GiaHienTai,
                    TongDaBan = group.Sum(ct => ct.SoLuong)
                })
                .OrderByDescending(x => x.TongDaBan)
                .Take(5)
                .ToList();

            // Trả về một PartialView (View con) kèm dữ liệu sản phẩm bán chạy
            return PartialView("_BestSellers", bestSellers);
        }

        [ChildActionOnly]
        //Tổng doanh thu tháng này
        public ActionResult _TotalRevenue()
        {
            // 1. Định nghĩa các trạng thái đơn hàng hợp lệ để tính doanh thu
            var validStatuses = new[] { "Đã xác nhận", "Đang giao", "Hoàn thành" };

            // 2. Xác định các mốc thời gian
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            // 3. Tính doanh thu tháng này từ bảng DONHANGs (hoặc db.DONHANG nếu db.DONHANGs báo lỗi)
            decimal doanhThuThangNay = db.DONHANGs
                .Where(d => validStatuses.Contains(d.TrangThai) && d.NgayDat >= startOfThisMonth && d.NgayDat <= now)
                .Sum(d => (decimal?)d.TongTien) ?? 0m;

            // 4. Tính doanh thu tháng trước
            decimal doanhThuThangTruoc = db.DONHANGs
                .Where(d => validStatuses.Contains(d.TrangThai) && d.NgayDat >= startOfLastMonth && d.NgayDat < startOfThisMonth)
                .Sum(d => (decimal?)d.TongTien) ?? 0m;

            // 5. Tính phần trăm tăng trưởng
            double phanTramTangTruong = 0;
            if (doanhThuThangTruoc > 0)
            {
                phanTramTangTruong = (double)((doanhThuThangNay - doanhThuThangTruoc) / doanhThuThangTruoc) * 100;
            }
            else if (doanhThuThangNay > 0)
            {
                phanTramTangTruong = 100; // Nếu tháng trước không bán được gì nhưng tháng này phát sinh doanh thu
            }

            // 6. Đóng gói dữ liệu vào ViewModel
            var model = new RevenueViewModel
            {
                TongDoanhThuThangNay = doanhThuThangNay,
                PhanTramTangTruong = Math.Abs(Math.Round(phanTramTangTruong, 1)), // Làm tròn 1 chữ số thập phân
                IsTangTruongDuong = phanTramTangTruong >= 0
            };

            return PartialView("_TotalRevenue", model);
        }

        [ChildActionOnly]
        //Tổng đơn hàng tháng này
        public ActionResult _TotalOrders()
        {
            // 1. Xác định mốc thời gian tháng này và tháng trước
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            // 2. Đếm tổng số đơn hàng tháng này
            int donHangThangNay = db.DONHANGs
                .Count(d => d.NgayDat >= startOfThisMonth && d.NgayDat <= now);

            // 3. Đếm tổng số đơn hàng tháng trước
            int donHangThangTruoc = db.DONHANGs
                .Count(d => d.NgayDat >= startOfLastMonth && d.NgayDat < startOfThisMonth);

            // 4. Tính toán phần trăm tăng trưởng
            double phanTramTangTruong = 0;
            if (donHangThangTruoc > 0)
            {
                // Ép kiểu chuẩn sang double để thực hiện phép chia
                phanTramTangTruong = (double)(donHangThangNay - donHangThangTruoc) / donHangThangTruoc * 100;
            }
            else if (donHangThangNay > 0)
            {
                phanTramTangTruong = 100;
            }

            // 5. Đóng gói vào Model
            var model = new OrderViewModel
            {
                TongDonHangThangNay = donHangThangNay,
                PhanTramTangTruong = Math.Abs(Math.Round(phanTramTangTruong, 1)), // Làm tròn 1 chữ số thập phân
                IsTangTruongDuong = phanTramTangTruong >= 0
            };

            return PartialView("_TotalOrders", model);
        }

        [ChildActionOnly]
        //Tổng sản phẩm bán ra tháng này
        public ActionResult _TotalProductSales()
        {
            // 1. Chỉ tính toán dựa trên các đơn hàng có trạng thái hợp lệ
            var validStatuses = new[] { "Đã xác nhận", "Đang giao", "Hoàn thành" };
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            // 2. Tính tổng số lượng sản phẩm bán ra tháng này
            // Truy vấn: Lấy các chi tiết đơn hàng thuộc về đơn hàng hợp lệ trong tháng này và SUM cột SoLuong
            int sanPhamThangNay = db.CHITIET_DONHANG 
                .Where(ct => validStatuses.Contains(ct.DONHANG.TrangThai)
                             && ct.DONHANG.NgayDat >= startOfThisMonth
                             && ct.DONHANG.NgayDat <= now)
                .Sum(ct => (int?)ct.SoLuong) ?? 0;

            // 3. Tính tổng số lượng sản phẩm bán ra tháng trước
            int sanPhamThangTruoc = db.CHITIET_DONHANG
                .Where(ct => validStatuses.Contains(ct.DONHANG.TrangThai)
                             && ct.DONHANG.NgayDat >= startOfLastMonth
                             && ct.DONHANG.NgayDat < startOfThisMonth)
                .Sum(ct => (int?)ct.SoLuong) ?? 0;

            // 4. Tính phần trăm tăng trưởng
            double phanTramTangTruong = 0;
            if (sanPhamThangTruoc > 0)
            {
                phanTramTangTruong = (double)(sanPhamThangNay - sanPhamThangTruoc) / sanPhamThangTruoc * 100;
            }
            else if (sanPhamThangNay > 0)
            {
                phanTramTangTruong = 100;
            }

            // 5. Đóng gói dữ liệu trả về View
            var model = new ProductSalesViewModel
            {
                TongSanPhamBanRaThangNay = sanPhamThangNay,
                PhanTramTangTruong = Math.Abs(Math.Round(phanTramTangTruong, 1)),
                IsTangTruongDuong = phanTramTangTruong >= 0
            };

            return PartialView("_TotalProductSales", model);
        }
        
        [ChildActionOnly]
        //Danh mục bán chạy
        public ActionResult _TopCategories()
        {
            // 1. Chỉ tính toán từ các đơn hàng thành công/hợp lệ
            var validStatuses = new[] { "Đã xác nhận", "Đang giao", "Hoàn thành" };

            // 2. Lấy dữ liệu chi tiết đơn hàng thỏa mãn điều kiện
            var validOrderDetails = db.CHITIET_DONHANG
                .Where(ct => validStatuses.Contains(ct.DONHANG.TrangThai));

            // 3. Tính tổng số lượng sản phẩm bán ra của tất cả danh mục để làm mẫu số
            int totalSold = validOrderDetails.Sum(ct => (int?)ct.SoLuong) ?? 0;

            // Nếu chưa bán được sản phẩm nào, trả về danh sách rỗng để tránh lỗi chia cho 0
            if (totalSold == 0)
            {
                return PartialView("_TopCategories", new List<CategorySalesViewModel>());
            }

            // 4. Nhóm theo Danh mục (DANHMUC) và tính tổng số lượng bán của từng nhóm, lấy Top 4 nhóm bán chạy nhất
            var topCategoriesData = validOrderDetails
                .GroupBy(ct => ct.SANPHAM.DANHMUC.TenDM) // <-- Sửa lại thuộc tính tên danh mục của bạn (ví dụ: TenDM hoặc TenDanhMuc)
                .Select(g => new
                {
                    TenDanhMuc = g.Key,
                    SoLuongBan = g.Sum(ct => (int?)ct.SoLuong) ?? 0
                })
                .OrderByDescending(g => g.SoLuongBan)
                .Take(4) // Chỉ lấy Top 4 danh mục bán chạy nhất
                .ToList();

            // 5. Khai báo danh sách các class màu progress bar theo thứ tự ưu tiên từ cao xuống thấp
            var colorClasses = new[] { "bg-success", "bg-warning", "bg-info", "bg-danger" };

            // 6. Đóng gói dữ liệu vào danh sách ViewModel
            var model = topCategoriesData.Select((item, index) => new CategorySalesViewModel
            {
                TenDanhMuc = item.TenDanhMuc,
                SoLuongBan = item.SoLuongBan,
                TyLePhanTram = Math.Round((double)item.SoLuongBan / totalSold * 100, 0), // Làm tròn không chữ số thập phân giống HTML tĩnh của bạn
                ColorClass = colorClasses[index % colorClasses.Length] // Gán màu động dựa trên thứ hạng
            }).ToList();

            return PartialView("_TopCategories", model);
        }
    }
}