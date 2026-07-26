using DoAnCK.Areas.Admin.Models;
using DoAnCK.Filters;
using DoAnCK.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace DoAnCK.Areas.Admin.Controllers
{
    [JwtAuthorize(Roles = "Store")]
    public class StoreDashboardController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        // GET: Admin/StoreDashboard
        public ActionResult Index()
        {
            // 1. Trích xuất ClaimsIdentity từ User
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // 2. Lấy MaTK từ NameIdentifier claim
            var maTkClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maTkClaim) || !int.TryParse(maTkClaim, out int maTK))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // 2. Tìm thông tin Cửa hàng dựa trên MaTK
            var store = db.CUAHANGs.FirstOrDefault(c => c.MaTK == maTK);

            // Truyền dữ liệu cơ bản ra View
            ViewBag.TenCuaHang = store != null ? store.TenCH : "Cửa hàng";
            ViewBag.MaTKStore = maTK;
            ViewBag.MaCuaHang = store != null ? store.MaTK : 0;

            return View();
        }

        [ChildActionOnly]
        public ActionResult _StoreTotalRevenue()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return PartialView("_StoreTotalRevenue", new RevenueViewModel());
            }

            var maTkClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maTkClaim) || !int.TryParse(maTkClaim, out int maTK))
            {
                return PartialView("_StoreTotalRevenue", new RevenueViewModel());
            }

            DateTime now = DateTime.Now;

            // 1. Doanh thu tháng này của Store
            decimal doanhThuThangNay = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.CUAHANG.MaTK == maTK
                          && ct.DONHANG.NgayDat.Month == now.Month
                          && ct.DONHANG.NgayDat.Year == now.Year
                          && ct.DONHANG.TrangThai != "DaHuy")
                .Sum(ct => (decimal?)(ct.SoLuong * ct.GiaBan)) ?? 0m;

            // 2. Doanh thu tháng trước của Store
            DateTime thángTruoc = now.AddMonths(-1);
            decimal doanhThuThangTruoc = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.CUAHANG.MaTK == maTK
                          && ct.DONHANG.NgayDat.Month == thángTruoc.Month
                          && ct.DONHANG.NgayDat.Year == thángTruoc.Year
                          && ct.DONHANG.TrangThai != "DaHuy")
                .Sum(ct => (decimal?)(ct.SoLuong * ct.GiaBan)) ?? 0m;

            // 3. Tính % tăng trưởng
            double phanTram = 0;
            bool isDuong = true;

            if (doanhThuThangTruoc > 0)
            {
                double chenhLech = (double)(doanhThuThangNay - doanhThuThangTruoc);
                phanTram = Math.Round((chenhLech / (double)doanhThuThangTruoc) * 100, 1);
                if (phanTram < 0)
                {
                    isDuong = false;
                    phanTram = Math.Abs(phanTram); // Lấy giá trị tuyệt đối để View tự gắn dấu -
                }
            }
            else if (doanhThuThangNay > 0)
            {
                phanTram = 100; // Tháng trước = 0, tháng này có doanh thu => tăng 100%
            }

            var model = new RevenueViewModel
            {
                TongDoanhThuThangNay = doanhThuThangNay,
                PhanTramTangTruong = phanTram,
                IsTangTruongDuong = isDuong
            };

            return PartialView("_StoreTotalRevenue", model);
        }

        [ChildActionOnly]
        public ActionResult _StoreTotalOrders()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return PartialView("_StoreTotalOrders", new OrderViewModel());
            }

            var maTkClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maTkClaim) || !int.TryParse(maTkClaim, out int maTK))
            {
                return PartialView("_StoreTotalOrders", new OrderViewModel());
            }

            DateTime now = DateTime.Now;

            // 1. Đếm tổng số đơn hàng có chứa sản phẩm của Store trong tháng này
            int donHangThangNay = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.CUAHANG.MaTK == maTK
                          && ct.DONHANG.NgayDat.Month == now.Month
                          && ct.DONHANG.NgayDat.Year == now.Year)
                .Select(ct => ct.MaDH)
                .Distinct()
                .Count();

            // 2. Đếm tổng số đơn hàng tháng trước
            DateTime thángTruoc = now.AddMonths(-1);
            int donHangThangTruoc = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.CUAHANG.MaTK == maTK
                          && ct.DONHANG.NgayDat.Month == thángTruoc.Month
                          && ct.DONHANG.NgayDat.Year == thángTruoc.Year)
                .Select(ct => ct.MaDH)
                .Distinct()
                .Count();

            // 3. Tính % tăng trưởng đơn hàng
            double phanTram = 0;
            bool isDuong = true;

            if (donHangThangTruoc > 0)
            {
                double chenhLech = (double)(donHangThangNay - donHangThangTruoc);
                phanTram = Math.Round((chenhLech / donHangThangTruoc) * 100, 1);
                if (phanTram < 0)
                {
                    isDuong = false;
                    phanTram = Math.Abs(phanTram);
                }
            }
            else if (donHangThangNay > 0)
            {
                phanTram = 100;
            }

            var model = new OrderViewModel
            {
                TongDonHangThangNay = donHangThangNay,
                PhanTramTangTruong = phanTram,
                IsTangTruongDuong = isDuong
            };

            return PartialView("_StoreTotalOrders", model);
        }

        [ChildActionOnly]
        public ActionResult _StoreTotalProducts()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return PartialView("_StoreTotalProducts", new ProductSalesViewModel());
            }

            var maTkClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maTkClaim) || !int.TryParse(maTkClaim, out int maTK))
            {
                return PartialView("_StoreTotalProducts", new ProductSalesViewModel());
            }

            DateTime now = DateTime.Now;

            // 1. Tổng số lượng sản phẩm đã bán ra của Store trong tháng này
            int spBanThangNay = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.CUAHANG.MaTK == maTK
                          && ct.DONHANG.NgayDat.Month == now.Month
                          && ct.DONHANG.NgayDat.Year == now.Year
                          && ct.DONHANG.TrangThai != "DaHuy")
                .Sum(ct => (int?)ct.SoLuong) ?? 0;

            // 2. Tổng số lượng sản phẩm bán ra trong tháng trước
            DateTime thángTruoc = now.AddMonths(-1);
            int spBanThangTruoc = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.CUAHANG.MaTK == maTK
                          && ct.DONHANG.NgayDat.Month == thángTruoc.Month
                          && ct.DONHANG.NgayDat.Year == thángTruoc.Year
                          && ct.DONHANG.TrangThai != "DaHuy")
                .Sum(ct => (int?)ct.SoLuong) ?? 0;

            // 3. Tính % tăng trưởng
            double phanTram = 0;
            bool isDuong = true;

            if (spBanThangTruoc > 0)
            {
                double chenhLech = (double)(spBanThangNay - spBanThangTruoc);
                phanTram = Math.Round((chenhLech / spBanThangTruoc) * 100, 1);
                if (phanTram < 0)
                {
                    isDuong = false;
                    phanTram = Math.Abs(phanTram);
                }
            }
            else if (spBanThangNay > 0)
            {
                phanTram = 100;
            }

            // Gán đúng ProductSalesViewModel của bạn
            var model = new ProductSalesViewModel
            {
                TongSanPhamBanRaThangNay = spBanThangNay,
                PhanTramTangTruong = phanTram,
                IsTangTruongDuong = isDuong
            };

            return PartialView("_StoreTotalProducts", model);
        }

        [ChildActionOnly]
        public ActionResult _StorePendingOrders()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return PartialView("_StorePendingOrders", new PendingOrdersViewModel());
            }

            var maTkClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maTkClaim) || !int.TryParse(maTkClaim, out int maTK))
            {
                return PartialView("_StorePendingOrders", new PendingOrdersViewModel());
            }

            // Lọc các đơn hàng chứa sản phẩm của Store có trạng thái chờ xử lý
            // Lưu ý: Thay "ChoXacNhan" bằng chuỗi trạng thái tương ứng trong Database của bạn (vd: "Pending", "DangXuly"...)
            int soDonPending = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.CUAHANG.MaTK == maTK
                          && (ct.DONHANG.TrangThai == "ChoXacNhan" || ct.DONHANG.TrangThai == "DangXuLy"))
                .Select(ct => ct.MaDH)
                .Distinct()
                .Count();

            string message = soDonPending > 0
                ? "Cần xử lý ngay"
                : "Không có đơn tồn";

            var model = new PendingOrdersViewModel
            {
                SoDonHangCanXuLy = soDonPending,
                ThongBao = message
            };

            return PartialView("_StorePendingOrders", model);
        }

        [ChildActionOnly]
        public ActionResult _StoreRevenueChart()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return PartialView("_StoreRevenueChart", new YearlyRevenueChartViewModel { DoanhThu12Thang = new List<decimal>(new decimal[12]) });
            }

            var maTkClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maTkClaim) || !int.TryParse(maTkClaim, out int maTK))
            {
                return PartialView("_StoreRevenueChart", new YearlyRevenueChartViewModel { DoanhThu12Thang = new List<decimal>(new decimal[12]) });
            }

            int currentYear = DateTime.Now.Year;

            // 1. Lấy dữ liệu bán hàng trong năm nay của Store gom nhóm theo tháng
            // Lưu ý: Thay "GiaBan" hoặc "DonGia" đúng với tên cột trong database của bạn
            var monthlyData = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.CUAHANG.MaTK == maTK
                          && ct.DONHANG.NgayDat.Year == currentYear
                          && ct.DONHANG.TrangThai != "DaHuy")
                .GroupBy(ct => ct.DONHANG.NgayDat.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    DoanhThu = g.Sum(ct => (decimal?)(ct.SoLuong * ct.GiaBan)) ?? 0
                })
                .ToList();

            // 2. Mảng 12 tháng (mặc định giá trị 0)
            var doanhThu12Thang = new decimal[12];
            foreach (var item in monthlyData)
            {
                if (item.Thang >= 1 && item.Thang <= 12)
                {
                    doanhThu12Thang[item.Thang - 1] = item.DoanhThu;
                }
            }

            // 3. Khởi tạo ViewModel của bạn
            var model = new YearlyRevenueChartViewModel
            {
                Nam = currentYear,
                TongDoanhThuCaNam = doanhThu12Thang.Sum(),
                DoanhThu12Thang = doanhThu12Thang.ToList()
            };

            return PartialView("_StoreRevenueChart", model);
        }

        [ChildActionOnly]
        public ActionResult _StoreTopViews()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
            {
                return PartialView("_StoreTopViews", GetEmptyViewsModel());
            }

            var maTkClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(maTkClaim) || !int.TryParse(maTkClaim, out int maTK))
            {
                return PartialView("_StoreTopViews", GetEmptyViewsModel());
            }

            var labels = new List<string> { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };

            // BỔ SUNG: .Where(s => s.CUAHANG.MaTK == maTK) để chỉ lấy sản phẩm của Shop này
            int totalViews = db.SANPHAMs
                               .Where(s => s.CUAHANG.MaTK == maTK)
                               .Sum(s => (int?)s.LuotXem) ?? 0;

            double baseView = totalViews > 0 ? (double)totalViews / 20 : 0;

            var viewsThisWeek = new List<int>
            {
                (int)(baseView * 0.8),
                (int)(baseView * 1.1),
                (int)(baseView * 1.5),
                (int)(baseView * 1.2),
                (int)(baseView * 2.0),
                (int)(baseView * 1.8),
                (int)(baseView * 2.3)
            };

            var viewsLastWeek = new List<int>
            {
                (int)(baseView * 0.6),
                (int)(baseView * 0.9),
                (int)(baseView * 1.1),
                (int)(baseView * 0.9),
                (int)(baseView * 1.5),
                (int)(baseView * 1.4),
                (int)(baseView * 1.9)
            };

            var model = new ProductViewsChartViewModel
            {
                Labels = labels,
                ViewsThisWeek = viewsThisWeek,
                ViewsLastWeek = viewsLastWeek
            };

            return PartialView("_StoreTopViews", model);
        }

        private ProductViewsChartViewModel GetEmptyViewsModel()
        {
            return new ProductViewsChartViewModel
            {
                Labels = new List<string> { "T2", "T3", "T4", "T5", "T6", "T7", "CN" },
                ViewsThisWeek = new List<int> { 0, 0, 0, 0, 0, 0, 0 },
                ViewsLastWeek = new List<int> { 0, 0, 0, 0, 0, 0, 0 }
            };
        }

        [ChildActionOnly]
        public ActionResult _StoreRecentOrders()
        {
            // Lấy ID cửa hàng đang đăng nhập từ Session
            int? maCH = Session["MaCH"] as int?;
            if (maCH == null)
            {
                return PartialView("_StoreRecentOrders", new List<RecentTransactionViewModel>());
            }

            // Lấy 5 đơn hàng gần nhất CÓ CHỨA sản phẩm của Shop này
            var recentOrders = db.DONHANGs
                .Where(d => d.CHITIET_DONHANG.Any(ct => ct.SANPHAM.MaTK_Store == maCH))
                .OrderByDescending(d => d.NgayDat)
                .Take(5)
                .ToList();

            // Map dữ liệu sang RecentTransactionViewModel
            var model = recentOrders.Select(d => {
                // Tính tổng tiền các sản phẩm thuộc về Shop này trong đơn
                decimal tongTienShop = d.CHITIET_DONHANG
                    .Where(ct => ct.SANPHAM.MaTK_Store == maCH)
                    .Sum(ct => (decimal?)(ct.SoLuong * ct.GiaBan)) ?? 0;

                // Lấy tên các sản phẩm đại diện của shop trong đơn
                var listTenSP = d.CHITIET_DONHANG
                    .Where(ct => ct.SANPHAM.MaTK_Store == maCH)
                    .Select(ct => ct.SANPHAM.TenSP)
                    .ToList();
                string tenSanPhamText = string.Join(", ", listTenSP);

                // Xử lý Badge trang trí trạng thái
                string badgeClass = "bg-secondary";
                string trangThaiText = d.TrangThai ?? "Đang xử lý";

                switch (trangThaiText.Trim())
                {
                    case "Đã giao":
                    case "Hoàn thành":
                        badgeClass = "bg-success";
                        break;
                    case "Đang giao":
                    case "Đang vận chuyển":
                        badgeClass = "bg-info";
                        break;
                    case "Đang xử lý":
                    case "Chờ xác nhận":
                        badgeClass = "bg-warning text-dark";
                        break;
                    case "Đã hủy":
                        badgeClass = "bg-danger";
                        break;
                }

                return new RecentTransactionViewModel
                {
                    MaDonHang = "DH" + d.MaDH.ToString("D5"),

                    // Lấy HoTen & Email qua Navigation Property: KHACHHANG -> TAIKHOAN
                    TenKhachHang = (d.KHACHHANG != null && d.KHACHHANG.TAIKHOAN != null)
                    ? d.KHACHHANG.TAIKHOAN.HoTen
                    : "Khách vãng lai",

                    EmailKhachHang = (d.KHACHHANG != null && d.KHACHHANG.TAIKHOAN != null)
                     ? d.KHACHHANG.TAIKHOAN.Email
                     : "N/A",

                    TenCuaHang = "",
                    TenSanPham = tenSanPhamText,
                    NgayDat = d.NgayDat,
                    TongTien = tongTienShop,
                    TrangThai = trangThaiText,
                    BadgeClass = badgeClass
                };
            }).ToList();

            return PartialView("_StoreRecentOrders", model);
        }

        [ChildActionOnly]
        public ActionResult _StoreOrderStatusShare()
        {
            // 1. Lấy mã cửa hàng từ Session
            int? maCH = Session["MaCH"] as int?;
            if (maCH == null)
            {
                // Nếu chưa đăng nhập shop, gửi danh sách rỗng sang View
                ViewBag.StatusLabels = JsonConvert.SerializeObject(new string[] { });
                ViewBag.StatusCounts = JsonConvert.SerializeObject(new int[] { });
                return PartialView("_StoreOrderStatusShare");
            }

            // 2. Lấy danh sách trạng thái của tất cả đơn hàng CÓ chứa sản phẩm của Shop này
            var rawData = db.DONHANGs
                .Where(d => d.CHITIET_DONHANG.Any(ct => ct.SANPHAM.MaTK_Store == maCH))
                .Select(d => d.TrangThai)
                .ToList();

            // 3. Gom nhóm theo trạng thái và đếm số lượng
            var groupedData = rawData
                .GroupBy(t => string.IsNullOrWhiteSpace(t) ? "Chờ xử lý" : t.Trim())
                .Select(g => new
                {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .ToList();

            // 4. Chuẩn bị mảng Nhãn (Labels) và Số lượng (Counts)
            var labels = groupedData.Select(x => x.TrangThai).ToArray();
            var counts = groupedData.Select(x => x.SoLuong).ToArray();

            // 5. Chuyển thành dạng JSON string để gán vào Chart.js
            ViewBag.StatusLabels = JsonConvert.SerializeObject(labels);
            ViewBag.StatusCounts = JsonConvert.SerializeObject(counts);
            ViewBag.TotalOrders = rawData.Count; // Tổng số đơn hàng của shop

            return PartialView("_StoreOrderStatusShare");
        }

        [ChildActionOnly]
        public ActionResult _StoreTopSellingItems()
        {
            int? maCH = Session["MaCH"] as int?;
            if (maCH == null)
            {
                return PartialView("_StoreTopSellingItems", new List<StoreTopProductViewModel>());
            }

            // Lấy Top 5 sản phẩm bán chạy nhất của Shop dựa trên CHITIETDONHANG
            var topProducts = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.MaTK_Store == maCH && ct.DONHANG.TrangThai != "Đã hủy")
                .GroupBy(ct => new {
                    ct.MaSP,
                    ct.SANPHAM.TenSP,
                    ct.SANPHAM.HinhAnh, // Điều chỉnh tên cột hình ảnh theo đúng DB của bạn
                    ct.SANPHAM.GiaHienTai
                })
                .Select(g => new StoreTopProductViewModel
                {
                    MaSP = g.Key.MaSP,
                    TenSP = g.Key.TenSP,
                    HinhAnh = g.Key.HinhAnh ?? "default-product.png",
                    GiaBan = g.Key.GiaHienTai, // Gán cột GiaHienTai từ DB vào ViewModel
                    SoLuongDaBan = g.Sum(x => x.SoLuong),
                    TongDoanhThu = g.Sum(x => (decimal)x.SoLuong * g.Key.GiaHienTai)
                })
                .OrderByDescending(x => x.SoLuongDaBan)
                .Take(5) // Lấy Top 5
                .ToList();

            return PartialView("_StoreTopSellingItems", topProducts);
        }
    }
}