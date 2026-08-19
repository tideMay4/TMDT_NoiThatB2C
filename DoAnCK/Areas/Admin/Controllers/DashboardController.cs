using DoAnCK.Areas.Admin.Models;
using DoAnCK.Filters;
using DoAnCK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnCK.Areas.Admin.Controllers
{
    [JwtAuthorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index()
        {
            return View();
        }

        [ChildActionOnly]
        // 1. Tổng doanh thu tháng này (Cột 1)
        public ActionResult _TotalRevenue()
        {
            var validStatuses = new[] { "Đã xác nhận", "Đang giao", "Hoàn thành" };

            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            decimal doanhThuThangNay = db.DONHANGs
                .Where(d => validStatuses.Contains(d.TrangThai) && d.NgayDat >= startOfThisMonth && d.NgayDat <= now)
                .Sum(d => (decimal?)d.TongTien) ?? 0m;

            decimal doanhThuThangTruoc = db.DONHANGs
                .Where(d => validStatuses.Contains(d.TrangThai) && d.NgayDat >= startOfLastMonth && d.NgayDat < startOfThisMonth)
                .Sum(d => (decimal?)d.TongTien) ?? 0m;

            double phanTramTangTruong = 0;
            if (doanhThuThangTruoc > 0)
            {
                phanTramTangTruong = (double)((doanhThuThangNay - doanhThuThangTruoc) / doanhThuThangTruoc) * 100;
            }
            else if (doanhThuThangNay > 0)
            {
                phanTramTangTruong = 100;
            }

            var model = new RevenueViewModel
            {
                TongDoanhThuThangNay = doanhThuThangNay,
                PhanTramTangTruong = Math.Abs(Math.Round(phanTramTangTruong, 1)),
                IsTangTruongDuong = phanTramTangTruong >= 0
            };

            return PartialView("_TotalRevenue", model);
        }

        [ChildActionOnly]
        // 2. Tổng đơn hàng tháng này (Cột 2)
        public ActionResult _TotalOrders()
        {
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            int donHangThangNay = db.DONHANGs
                .Count(d => d.NgayDat >= startOfThisMonth && d.NgayDat <= now);

            int donHangThangTruoc = db.DONHANGs
                .Count(d => d.NgayDat >= startOfLastMonth && d.NgayDat < startOfThisMonth);

            double phanTramTangTruong = 0;
            if (donHangThangTruoc > 0)
            {
                phanTramTangTruong = (double)(donHangThangNay - donHangThangTruoc) / donHangThangTruoc * 100;
            }
            else if (donHangThangNay > 0)
            {
                phanTramTangTruong = 100;
            }

            var model = new OrderViewModel
            {
                TongDonHangThangNay = donHangThangNay,
                PhanTramTangTruong = Math.Abs(Math.Round(phanTramTangTruong, 1)),
                IsTangTruongDuong = phanTramTangTruong >= 0
            };

            return PartialView("_TotalOrders", model);
        }

        [ChildActionOnly]
        // Cột 3: Tổng số Cửa hàng đang có
        public ActionResult _TotalActiveStores()
        {
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);

            int totalStores = db.CUAHANGs.Count();
            int newStoresThisMonth = db.CUAHANGs
                .Count(c => c.NgayThanhLap >= startOfThisMonth && c.NgayThanhLap <= now);

            var model = new StoreViewModel
            {
                TongSoCuaHang = totalStores,
                SoCuaHangMoiThangNay = newStoresThisMonth
            };

            return PartialView("_TotalActiveStores", model);
        }

        [ChildActionOnly]
        // Cột 4: Tổng số Khách hàng / Người dùng
        public ActionResult _TotalCustomers()
        {
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            // Giả định vai trò Khách hàng là MaLoai = 2 hoặc Vaitro = "Customer" 
            // Nếu đếm toàn bộ người dùng thì bỏ điều kiện lọc role
            int khachHangThangNay = db.TAIKHOANs.Count(u => u.NgayTao <= now);
            int khachHangThangTruoc = db.TAIKHOANs.Count(u => u.NgayTao < startOfThisMonth);

            double phanTramTangTruong = 0;
            if (khachHangThangTruoc > 0)
            {
                phanTramTangTruong = (double)(khachHangThangNay - khachHangThangTruoc) / khachHangThangTruoc * 100;
            }
            else if (khachHangThangNay > 0)
            {
                phanTramTangTruong = 100;
            }

            var model = new CustomerViewModel
            {
                TongKhachHangThangNay = khachHangThangNay,
                PhanTramTangTruong = Math.Abs(Math.Round(phanTramTangTruong, 1)),
                IsTangTruongDuong = phanTramTangTruong >= 0
            };

            return PartialView("_TotalCustomers", model);
        }

        [ChildActionOnly]
        //Biểu đồ doanh thu sàn
        public ActionResult _YearlyRevenueChart()
        {
            int currentYear = DateTime.Now.Year;
            var validStatuses = new[] { "Đã xác nhận", "Đang giao", "Hoàn thành" };

            // Lấy doanh thu theo từng tháng
            var yearlyData = db.DONHANGs
                .Where(d => validStatuses.Contains(d.TrangThai) && d.NgayDat.Year == currentYear)
                .GroupBy(d => d.NgayDat.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    TongTien = g.Sum(d => (decimal?)d.TongTien) ?? 0m
                })
                .ToList();

            List<decimal> monthlyRevenue = new List<decimal>(new decimal[12]);
            decimal totalYearRevenue = 0m;

            foreach (var item in yearlyData)
            {
                monthlyRevenue[item.Thang - 1] = item.TongTien;
                totalYearRevenue += item.TongTien;
            }

            var model = new YearlyRevenueChartViewModel
            {
                Nam = currentYear,
                TongDoanhThuCaNam = totalYearRevenue,
                DoanhThu12Thang = monthlyRevenue
            };

            return PartialView("_YearlyRevenueChart", model);
        }

        [ChildActionOnly]
        public ActionResult _TopProductViews()
        {
            var labels = new List<string> { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };

            // 1. Sửa lỗi int??: Dùng Sum(s => (int?)s.LuotXem) để ép kiểu nullable an toàn khi DB trống
            int totalViews = db.SANPHAMs.Sum(s => (int?)s.LuotXem) ?? 0;

            // 2. Sửa lỗi ViewModel không tồn tại: Dùng Anonymous Type (new { ... })
            // Sửa lỗi ?? ở LuotXem bằng cách loại bỏ ?? 0 nếu LuotXem là int
            var compareViews = db.SANPHAMs
                .GroupBy(s => s.TenSP)
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    TenSanPham = g.Key,
                    DanhSachCuaHang = g.Select(s => new
                    {
                        TenCuaHang = s.CUAHANG.TenCH,
                        LuotXem = s.LuotXem
                    })
                    .OrderByDescending(x => x.LuotXem)
                    .ToList()
                })
                .ToList();

            ViewBag.CompareViews = compareViews;

            // 3. Phân bổ dữ liệu biểu đồ
            double baseView = totalViews > 0 ? (double)totalViews / 20 : 0;

            var viewsThisWeek = new List<int>
            {
                (int)(baseView * 0.8), (int)(baseView * 1.1), (int)(baseView * 1.5),
                (int)(baseView * 1.2), (int)(baseView * 2.0), (int)(baseView * 1.8), (int)(baseView * 2.3)
            };

            var viewsLastWeek = new List<int>
            {
                (int)(baseView * 0.6), (int)(baseView * 0.9), (int)(baseView * 1.1),
                (int)(baseView * 0.9), (int)(baseView * 1.5), (int)(baseView * 1.4), (int)(baseView * 1.9)
            };

            var model = new ProductViewsChartViewModel
            {
                Labels = labels,
                ViewsThisWeek = viewsThisWeek,
                ViewsLastWeek = viewsLastWeek
            };

            return PartialView("_TopProductViews", model);
        }

        [ChildActionOnly]
        public ActionResult _RecentTransactions()
        {
            var model = GetRecentTransactionsData();
            return PartialView("_RecentTransactions", model);
        }

        // Action hỗ trợ gọi AJAX lấy dữ liệu real-time
        [HttpGet]
        public ActionResult GetRecentTransactionsJson()
        {
            var model = GetRecentTransactionsData();
            return PartialView("_RecentTransactions", model);
        }

        private List<RecentTransactionViewModel> GetRecentTransactionsData()
        {
            // Lấy top 5 đơn hàng mới nhất
            var recentOrders = db.DONHANGs
                .OrderByDescending(d => d.NgayDat)
                .Take(5)
                .ToList();

            var result = new List<RecentTransactionViewModel>();

            foreach (var item in recentOrders)
            {
                // 1. Lấy thông tin Khách hàng từ KHACHHANG -> TAIKHOAN
                string customerName = "Khách vãng lai";
                string customerEmail = "khach@gmail.com";
                int accountId = 0;

                if (item.KHACHHANG != null && item.KHACHHANG.TAIKHOAN != null)
                {
                    customerName = item.KHACHHANG.TAIKHOAN.HoTen;
                    customerEmail = item.KHACHHANG.TAIKHOAN.Email;
                    accountId = item.KHACHHANG.TAIKHOAN.MaTK;
                }

                // 2. Lấy tên Sản phẩm đầu tiên trong đơn
                var firstDetail = item.CHITIET_DONHANG.FirstOrDefault();
                string productName = firstDetail != null && firstDetail.SANPHAM != null
                    ? firstDetail.SANPHAM.TenSP
                    : "Sản phẩm sàn";

                // 3. Lấy tên Cửa hàng (CUAHANG) sở hữu sản phẩm đó
                string storeName = "Sàn Commerce";
                if (firstDetail != null && firstDetail.SANPHAM != null)
                {
                    // Bảng SANPHAM lưu MaTK của Cửa hàng
                    var store = db.CUAHANGs.FirstOrDefault(c => c.MaTK == firstDetail.SANPHAM.MaTK_Store);
                    if (store != null)
                    {
                        storeName = store.TenCH;
                    }
                }

                // 4. Mapping Class Badge hiển thị trạng thái
                string badge = "badge-warning";
                if (item.TrangThai == "Hoàn thành") badge = "badge-success";
                else if (item.TrangThai == "Đã hủy") badge = "badge-danger";

                result.Add(new RecentTransactionViewModel
                {
                    MaDonHang = "#ORD-" + item.MaDH,
                    TenKhachHang = customerName,
                    EmailKhachHang = customerEmail,
                    TenCuaHang = storeName,
                    TenSanPham = productName,
                    NgayDat = item.NgayDat,
                    TongTien = item.TongTien,
                    TrangThai = item.TrangThai,
                    BadgeClass = badge
                });
            }

            return result;
        }

        // 1. Partial View Khối Bên Trái: % Sản phẩm bán ra của từng Cửa Hàng
        [ChildActionOnly]
        public ActionResult _StoreSalesShare()
        {
            // Lấy tổng số lượng sản phẩm bán ra thành công toàn sàn
            var validStatuses = new[] { "Đã xác nhận", "Đang giao", "Hoàn thành" };

            var storeSales = db.CHITIET_DONHANG
                .Where(ct => validStatuses.Contains(ct.DONHANG.TrangThai))
                .GroupBy(ct => ct.SANPHAM.MaTK_Store) // Group theo Cửa Hàng (MaTK)
                .Select(g => new
                {
                    MaTK = g.Key,
                    TongSoLuong = g.Sum(ct => ct.SoLuong)
                })
                .ToList();

            int totalAllStores = storeSales.Sum(s => s.TongSoLuong);
            if (totalAllStores == 0) totalAllStores = 1; // Tránh chia cho 0

            // Palette màu earth-tone ấm cúng đồng bộ Admin
            string[] warmColors = new string[] { "#7A5C43", "#8B5E3C", "#B08968", "#D99B6A", "#E6CCB2" };

            var model = new List<StoreShareViewModel>();
            int colorIndex = 0;

            foreach (var item in storeSales)
            {
                var store = db.CUAHANGs.FirstOrDefault(c => c.MaTK == item.MaTK);
                double percentage = System.Math.Round((double)item.TongSoLuong / totalAllStores * 100, 1);

                model.Add(new StoreShareViewModel
                {
                    TenCuaHang = store != null ? store.TenCH : "Cửa hàng #" + item.MaTK,
                    SoLuongDaBan = item.TongSoLuong,
                    TiLePhanTram = percentage,
                    ColorHex = warmColors[colorIndex % warmColors.Length]
                });
                colorIndex++;
            }

            return PartialView("_StoreSalesShare", model);
        }

        // 2. Partial View Khối Bên Phải: Top Sold Items (So sánh sản phẩm giữa các Shop)
        [ChildActionOnly]
        public ActionResult _TopSoldItemsCompare()
        {
            var stores = db.Database.SqlQuery<DashboardStoreMiniViewModel>(
                @"SELECT 
              MaTK AS MaStore,
              TenCH AS TenCuaHang
          FROM CUAHANG
          ORDER BY MaTK"
            ).ToList();

            var rows = db.Database.SqlQuery<DashboardTopSoldProductViewModel>(
                @"SELECT 
              CT.MaTK_Store AS MaStore,
              CH.TenCH AS TenCuaHang,
              SP.TenSP,
              SUM(CT.SoLuong) AS SoLuongDaBan,
              SUM(CT.ThanhTien) AS DoanhThu
          FROM CHITIET_DONHANG CT
          INNER JOIN DONHANG DH ON CT.MaDH = DH.MaDH
          INNER JOIN SANPHAM SP ON CT.MaSP = SP.MaSP
          INNER JOIN CUAHANG CH ON CT.MaTK_Store = CH.MaTK
          WHERE 
              CT.MaTK_Store IS NOT NULL
              AND ISNULL(DH.TrangThai, N'') <> N'Đã hủy'
          GROUP BY 
              CT.MaTK_Store,
              CH.TenCH,
              SP.TenSP
          ORDER BY 
              CT.MaTK_Store,
              SUM(CT.SoLuong) DESC"
            ).ToList();

            var model = stores
                .Take(2)
                .Select(store => new DashboardStoreTopSoldGroupViewModel
                {
                    MaStore = store.MaStore,
                    TenCuaHang = store.TenCuaHang,
                    TotalSold = rows
                        .Where(x => x.MaStore == store.MaStore)
                        .Sum(x => x.SoLuongDaBan),
                    TotalRevenue = rows
                        .Where(x => x.MaStore == store.MaStore)
                        .Sum(x => x.DoanhThu),
                    Products = rows
                        .Where(x => x.MaStore == store.MaStore)
                        .OrderByDescending(x => x.SoLuongDaBan)
                        .Take(5)
                        .ToList()
                })
                .ToList();

            return PartialView(model);
        }
    }

    public class DashboardStoreMiniViewModel
    {
        public int MaStore { get; set; }

        public string TenCuaHang { get; set; }
    }

    public class DashboardTopSoldProductViewModel
    {
        public int MaStore { get; set; }

        public string TenCuaHang { get; set; }

        public string TenSP { get; set; }

        public int SoLuongDaBan { get; set; }

        public decimal DoanhThu { get; set; }
    }

    public class DashboardStoreTopSoldGroupViewModel
    {
        public int MaStore { get; set; }

        public string TenCuaHang { get; set; }

        public int TotalSold { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<DashboardTopSoldProductViewModel> Products { get; set; }
    }
}