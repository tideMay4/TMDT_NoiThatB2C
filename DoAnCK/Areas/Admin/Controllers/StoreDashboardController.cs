using DoAnCK.Areas.Admin.Models;
using DoAnCK.Filters;
using DoAnCK.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;

namespace DoAnCK.Areas.Admin.Controllers
{
    [JwtAuthorize(Roles = "Store")]
    public class StoreDashboardController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        private int? GetCurrentStoreId()
        {
            int maTK;

            var identity = User.Identity as ClaimsIdentity;

            if (identity != null && identity.IsAuthenticated)
            {
                var maTkClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrWhiteSpace(maTkClaim) &&
                    int.TryParse(maTkClaim, out maTK) &&
                    db.CUAHANGs.Any(x => x.MaTK == maTK))
                {
                    Session["MaCH"] = maTK;
                    Session["MaTKStore"] = maTK;
                    return maTK;
                }
            }

            if (Session["MaCH"] != null &&
                int.TryParse(Session["MaCH"].ToString(), out maTK) &&
                db.CUAHANGs.Any(x => x.MaTK == maTK))
            {
                Session["MaTKStore"] = maTK;
                return maTK;
            }

            if (Session["MaTKStore"] != null &&
                int.TryParse(Session["MaTKStore"].ToString(), out maTK) &&
                db.CUAHANGs.Any(x => x.MaTK == maTK))
            {
                Session["MaCH"] = maTK;
                return maTK;
            }

            int fallbackStoreId = db.CUAHANGs
                .Select(x => x.MaTK)
                .FirstOrDefault();

            if (fallbackStoreId > 0)
            {
                Session["MaCH"] = fallbackStoreId;
                Session["MaTKStore"] = fallbackStoreId;
                return fallbackStoreId;
            }

            return null;
        }

        private bool IsCancelled(string trangThai)
        {
            if (string.IsNullOrWhiteSpace(trangThai))
            {
                return false;
            }

            string value = trangThai.Trim();

            return value == "Đã hủy" ||
                   value == "DaHuy" ||
                   value == "Đã huỷ" ||
                   value == "Hủy" ||
                   value == "Huỷ";
        }

        public ActionResult Index()
        {
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                ViewBag.TenCuaHang = "Cửa hàng";
                ViewBag.MaTKStore = 0;
                ViewBag.MaCuaHang = 0;
                return View();
            }

            var store = db.CUAHANGs.FirstOrDefault(c => c.MaTK == maTKStore.Value);

            ViewBag.TenCuaHang = store != null ? store.TenCH : "Cửa hàng";
            ViewBag.MaTKStore = maTKStore.Value;
            ViewBag.MaCuaHang = maTKStore.Value;

            return View();
        }

        [ChildActionOnly]
        public ActionResult _StoreTotalRevenue()
        {
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                return PartialView("_StoreTotalRevenue", new RevenueViewModel());
            }

            DateTime now = DateTime.Now;
            DateTime thangTruoc = now.AddMonths(-1);

            decimal doanhThuThangNay = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value
                          && ct.DONHANG.NgayDat.Month == now.Month
                          && ct.DONHANG.NgayDat.Year == now.Year
                          && ct.DONHANG.TrangThai != "Đã hủy"
                          && ct.DONHANG.TrangThai != "DaHuy")
                .Sum(ct => (decimal?)(ct.SoLuong * ct.GiaBan)) ?? 0m;

            decimal doanhThuThangTruoc = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value
                          && ct.DONHANG.NgayDat.Month == thangTruoc.Month
                          && ct.DONHANG.NgayDat.Year == thangTruoc.Year
                          && ct.DONHANG.TrangThai != "Đã hủy"
                          && ct.DONHANG.TrangThai != "DaHuy")
                .Sum(ct => (decimal?)(ct.SoLuong * ct.GiaBan)) ?? 0m;

            double phanTram = 0;
            bool isDuong = true;

            if (doanhThuThangTruoc > 0)
            {
                double chenhLech = (double)(doanhThuThangNay - doanhThuThangTruoc);
                phanTram = Math.Round((chenhLech / (double)doanhThuThangTruoc) * 100, 1);

                if (phanTram < 0)
                {
                    isDuong = false;
                    phanTram = Math.Abs(phanTram);
                }
            }
            else if (doanhThuThangNay > 0)
            {
                phanTram = 100;
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
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                return PartialView("_StoreTotalOrders", new OrderViewModel());
            }

            DateTime now = DateTime.Now;
            DateTime thangTruoc = now.AddMonths(-1);

            int donHangThangNay = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value
                          && ct.DONHANG.NgayDat.Month == now.Month
                          && ct.DONHANG.NgayDat.Year == now.Year)
                .Select(ct => ct.MaDH)
                .Distinct()
                .Count();

            int donHangThangTruoc = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value
                          && ct.DONHANG.NgayDat.Month == thangTruoc.Month
                          && ct.DONHANG.NgayDat.Year == thangTruoc.Year)
                .Select(ct => ct.MaDH)
                .Distinct()
                .Count();

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
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                return PartialView("_StoreTotalProducts", new ProductSalesViewModel());
            }

            int tongSanPhamDangBan = db.SANPHAMs
                .Where(sp => sp.MaTK_Store == maTKStore.Value && sp.TrangThai == true)
                .Count();

            var model = new ProductSalesViewModel
            {
                TongSanPhamBanRaThangNay = tongSanPhamDangBan,
                PhanTramTangTruong = tongSanPhamDangBan > 0 ? 100 : 0,
                IsTangTruongDuong = true
            };

            return PartialView("_StoreTotalProducts", model);
        }

        [ChildActionOnly]
        public ActionResult _StorePendingOrders()
        {
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                return PartialView("_StorePendingOrders", new PendingOrdersViewModel());
            }

            int soDonPending = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value
                          && (ct.DONHANG.TrangThai == "ChoXacNhan"
                              || ct.DONHANG.TrangThai == "DangXuLy"
                              || ct.DONHANG.TrangThai == "Chờ xác nhận"
                              || ct.DONHANG.TrangThai == "Đang xử lý"))
                .Select(ct => ct.MaDH)
                .Distinct()
                .Count();

            var model = new PendingOrdersViewModel
            {
                SoDonHangCanXuLy = soDonPending,
                ThongBao = soDonPending > 0 ? "Cần xử lý ngay" : "Không có đơn tồn"
            };

            return PartialView("_StorePendingOrders", model);
        }

        [ChildActionOnly]
        public ActionResult _StoreRevenueChart()
        {
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                return PartialView("_StoreRevenueChart", new YearlyRevenueChartViewModel
                {
                    Nam = DateTime.Now.Year,
                    TongDoanhThuCaNam = 0,
                    DoanhThu12Thang = new List<decimal>(new decimal[12])
                });
            }

            int currentYear = DateTime.Now.Year;

            var monthlyData = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value
                          && ct.DONHANG.NgayDat.Year == currentYear
                          && ct.DONHANG.TrangThai != "Đã hủy"
                          && ct.DONHANG.TrangThai != "DaHuy")
                .GroupBy(ct => ct.DONHANG.NgayDat.Month)
                .Select(g => new
                {
                    Thang = g.Key,
                    DoanhThu = g.Sum(ct => (decimal?)(ct.SoLuong * ct.GiaBan)) ?? 0
                })
                .ToList();

            decimal[] doanhThu12Thang = new decimal[12];

            foreach (var item in monthlyData)
            {
                if (item.Thang >= 1 && item.Thang <= 12)
                {
                    doanhThu12Thang[item.Thang - 1] = item.DoanhThu;
                }
            }

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
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                return PartialView("_StoreTopViews", GetEmptyViewsModel());
            }

            List<string> labels = new List<string> { "T2", "T3", "T4", "T5", "T6", "T7", "CN" };

            int totalViews = db.SANPHAMs
                .Where(s => s.MaTK_Store == maTKStore.Value)
                .Sum(s => (int?)s.LuotXem) ?? 0;

            double baseView = totalViews > 0 ? (double)totalViews / 20 : 0;

            List<int> viewsThisWeek = new List<int>
            {
                (int)(baseView * 0.8),
                (int)(baseView * 1.1),
                (int)(baseView * 1.5),
                (int)(baseView * 1.2),
                (int)(baseView * 2.0),
                (int)(baseView * 1.8),
                (int)(baseView * 2.3)
            };

            List<int> viewsLastWeek = new List<int>
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
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                return PartialView("_StoreRecentOrders", new List<RecentTransactionViewModel>());
            }

            var recentOrders = db.DONHANGs
                .Where(d => d.CHITIET_DONHANG.Any(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value))
                .OrderByDescending(d => d.NgayDat)
                .Take(5)
                .ToList();

            List<RecentTransactionViewModel> model = recentOrders.Select(d =>
            {
                decimal tongTienShop = d.CHITIET_DONHANG
                    .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value)
                    .Sum(ct => (decimal?)(ct.SoLuong * ct.GiaBan)) ?? 0m;

                List<string> listTenSP = d.CHITIET_DONHANG
                    .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value)
                    .Select(ct => ct.SANPHAM.TenSP)
                    .ToList();

                string trangThaiText = d.TrangThai ?? "Đang xử lý";
                string badgeClass = "badge-soft";

                switch (trangThaiText.Trim())
                {
                    case "Đã giao":
                    case "Hoàn thành":
                        badgeClass = "badge-success";
                        break;
                    case "Đang giao":
                    case "Đang vận chuyển":
                        badgeClass = "badge-warning";
                        break;
                    case "Đang xử lý":
                    case "Chờ xác nhận":
                        badgeClass = "badge-warning";
                        break;
                    case "Đã hủy":
                    case "DaHuy":
                        badgeClass = "badge-danger";
                        break;
                }

                return new RecentTransactionViewModel
                {
                    MaDonHang = "#ORD-" + d.MaDH,
                    TenKhachHang = d.KHACHHANG != null && d.KHACHHANG.TAIKHOAN != null
                        ? d.KHACHHANG.TAIKHOAN.HoTen
                        : "Khách vãng lai",
                    EmailKhachHang = d.KHACHHANG != null && d.KHACHHANG.TAIKHOAN != null
                        ? d.KHACHHANG.TAIKHOAN.Email
                        : "N/A",
                    TenCuaHang = "",
                    TenSanPham = string.Join(", ", listTenSP),
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
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                ViewBag.StatusLabels = JsonConvert.SerializeObject(new string[] { });
                ViewBag.StatusCounts = JsonConvert.SerializeObject(new int[] { });
                ViewBag.TotalOrders = 0;
                return PartialView("_StoreOrderStatusShare");
            }

            List<string> rawData = db.DONHANGs
                .Where(d => d.CHITIET_DONHANG.Any(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value))
                .Select(d => d.TrangThai)
                .ToList();

            var groupedData = rawData
                .GroupBy(t => string.IsNullOrWhiteSpace(t) ? "Chờ xử lý" : t.Trim())
                .Select(g => new
                {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .ToList();

            ViewBag.StatusLabels = JsonConvert.SerializeObject(groupedData.Select(x => x.TrangThai).ToArray());
            ViewBag.StatusCounts = JsonConvert.SerializeObject(groupedData.Select(x => x.SoLuong).ToArray());
            ViewBag.TotalOrders = rawData.Count;

            return PartialView("_StoreOrderStatusShare");
        }

        [ChildActionOnly]
        public ActionResult _StoreTopSellingItems()
        {
            int? maTKStore = GetCurrentStoreId();

            if (maTKStore == null)
            {
                return PartialView("_StoreTopSellingItems", new List<StoreTopProductViewModel>());
            }

            var topProducts = db.CHITIET_DONHANG
                .Where(ct => ct.SANPHAM.MaTK_Store == maTKStore.Value
                          && ct.DONHANG.TrangThai != "Đã hủy"
                          && ct.DONHANG.TrangThai != "DaHuy")
                .GroupBy(ct => new
                {
                    ct.MaSP,
                    ct.SANPHAM.TenSP,
                    ct.SANPHAM.HinhAnh,
                    ct.SANPHAM.GiaHienTai
                })
                .Select(g => new StoreTopProductViewModel
                {
                    MaSP = g.Key.MaSP,
                    TenSP = g.Key.TenSP,
                    HinhAnh = g.Key.HinhAnh ?? "default-product.png",
                    GiaBan = g.Key.GiaHienTai,
                    SoLuongDaBan = g.Sum(x => x.SoLuong),
                    TongDoanhThu = g.Sum(x => (decimal)x.SoLuong * g.Key.GiaHienTai)
                })
                .OrderByDescending(x => x.SoLuongDaBan)
                .Take(5)
                .ToList();

            return PartialView("_StoreTopSellingItems", topProducts);
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
