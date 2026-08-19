using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using DoAnCK.Models;

namespace DoAnCK.Controllers
{
    public class ProductController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index(string slug, string keyword, string sort)
        {
            var categories = db.DANHMUCs
                .Where(x => x.TrangThai == true)
                .OrderBy(x => x.TenDM)
                .ToList();

            var categoryCounts = db.SANPHAMs
                .Where(x => x.TrangThai == true)
                .GroupBy(x => x.MaDM)
                .ToDictionary(x => x.Key, x => x.Count());

            int totalProducts = db.SANPHAMs.Count(x => x.TrangThai == true);

            var productsQuery = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .Where(x => x.TrangThai == true);

            DANHMUC selectedCategory = null;

            if (!string.IsNullOrWhiteSpace(slug))
            {
                selectedCategory = categories.FirstOrDefault(x => x.Slug == slug);

                if (selectedCategory == null)
                {
                    return HttpNotFound();
                }

                productsQuery = productsQuery.Where(x => x.MaDM == selectedCategory.MaDM);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                productsQuery = productsQuery.Where(x =>
                    x.TenSP.Contains(keyword) ||
                    x.Slug.Contains(keyword) ||
                    x.MoTa.Contains(keyword) ||
                    x.MetaTitle.Contains(keyword) ||
                    x.MetaDescription.Contains(keyword) ||
                    x.MetaKeyword.Contains(keyword)
                );
            }

            switch (sort)
            {
                case "price-asc":
                    productsQuery = productsQuery.OrderBy(x => x.GiaHienTai);
                    break;

                case "price-desc":
                    productsQuery = productsQuery.OrderByDescending(x => x.GiaHienTai);
                    break;

                case "name-asc":
                    productsQuery = productsQuery.OrderBy(x => x.TenSP);
                    break;

                default:
                    productsQuery = productsQuery
                        .OrderByDescending(x => x.NgayTao)
                        .ThenByDescending(x => x.MaSP);
                    break;
            }

            var products = productsQuery.ToList();

            ViewBag.Categories = categories;
            ViewBag.CategoryCounts = categoryCounts;
            ViewBag.SelectedCategory = selectedCategory;
            ViewBag.Keyword = keyword;
            ViewBag.Sort = sort;
            ViewBag.TotalProducts = totalProducts;

            return View(products);
        }

        public ActionResult Detail(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return RedirectToAction("Index");
            }

            var product = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .FirstOrDefault(x => x.Slug == slug && x.TrangThai == true);

            if (product == null)
            {
                return HttpNotFound();
            }

            product.LuotXem = product.LuotXem + 1;
            db.SaveChanges();

            var storeOptions = db.Database.SqlQuery<StoreProductOptionViewModel>(
                @"SELECT 
                      CSP.MaTK_Store,
                      CH.TenCH AS TenCuaHang,
                      CSP.GiaBan,
                      CSP.SoLuongTon
                  FROM CUAHANG_SANPHAM CSP
                  INNER JOIN CUAHANG CH ON CSP.MaTK_Store = CH.MaTK
                  WHERE CSP.MaSP = @p0
                    AND CSP.TrangThai = 1
                    AND CSP.SoLuongTon > 0
                  ORDER BY CSP.GiaBan ASC",
                product.MaSP
            ).ToList();

            ViewBag.StoreOptions = storeOptions;

            int soLuotMua = 0;

            try
            {
                soLuotMua = db.Database.SqlQuery<int>(
                    "SELECT ISNULL(SUM(SoLuong), 0) FROM CHITIET_DONHANG WHERE MaSP = @p0",
                    product.MaSP
                ).FirstOrDefault();
            }
            catch
            {
                soLuotMua = 0;
            }

            int maKH = 0;
            bool daDangNhap = false;
            bool daMuaSanPham = false;
            bool daDanhGia = false;
            bool daYeuThich = false;

            if (Session["MaKH"] != null)
            {
                maKH = Convert.ToInt32(Session["MaKH"]);
                daDangNhap = true;

                try
                {
                    daMuaSanPham = db.Database.SqlQuery<int>(
                        @"SELECT COUNT(*)
                          FROM DONHANG DH
                          INNER JOIN CHITIET_DONHANG CT ON DH.MaDH = CT.MaDH
                          WHERE DH.MaKH = @p0
                          AND CT.MaSP = @p1
                          AND DH.TrangThai IN (N'Hoàn thành', N'Đã giao', N'Đã thanh toán')",
                        maKH,
                        product.MaSP
                    ).FirstOrDefault() > 0;
                }
                catch
                {
                    daMuaSanPham = false;
                }

                daDanhGia = db.DANHGIAs.Any(x =>
                    x.MaKH == maKH &&
                    x.MaSP == product.MaSP &&
                    x.TrangThai == true
                );

                daYeuThich = db.Database.SqlQuery<int>(
                    @"SELECT COUNT(*) 
                      FROM YEUTHICH 
                      WHERE MaKH = @p0 AND MaSP = @p1",
                    maKH,
                    product.MaSP
                ).FirstOrDefault() > 0;
            }

            var reviews = db.DANHGIAs
                .Where(x => x.MaSP == product.MaSP && x.TrangThai == true)
                .OrderByDescending(x => x.NgayDanhGia)
                .ToList()
                .Select(x => new ProductReviewViewModel
                {
                    MaDG = x.MaDG,
                    MaSP = x.MaSP,
                    MaKH = x.MaKH,
                    TenKhachHang = "Khách hàng #" + x.MaKH,
                    SoSao = x.SoSao,
                    BinhLuan = x.NoiDung,
                    NgayDanhGia = x.NgayDanhGia
                })
                .ToList();

            double diemTrungBinh = reviews.Any()
                ? reviews.Average(x => x.SoSao)
                : 0;

            var relatedProducts = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .Where(x =>
                    x.TrangThai == true &&
                    x.MaSP != product.MaSP &&
                    x.MaDM == product.MaDM)
                .OrderByDescending(x => x.NgayTao)
                .Take(4)
                .ToList();

            if (relatedProducts.Count < 4)
            {
                var existingIds = relatedProducts.Select(x => x.MaSP).ToList();
                existingIds.Add(product.MaSP);

                var moreProducts = db.SANPHAMs
                    .Include(x => x.DANHMUC)
                    .Where(x =>
                        x.TrangThai == true &&
                        !existingIds.Contains(x.MaSP))
                    .OrderByDescending(x => x.NgayTao)
                    .Take(4 - relatedProducts.Count)
                    .ToList();

                relatedProducts.AddRange(moreProducts);
            }

            var model = new ProductDetailViewModel
            {
                Product = product,
                SoLuotMua = soLuotMua,
                TongDanhGia = reviews.Count,
                DiemTrungBinh = diemTrungBinh,
                DaDangNhap = daDangNhap,
                DaMuaSanPham = daMuaSanPham,
                DaDanhGia = daDanhGia,
                DaYeuThich = daYeuThich,
                Reviews = reviews,
                RelatedProducts = relatedProducts
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(int MaSP, int SoSao, string BinhLuan)
        {
            var product = db.SANPHAMs.Find(MaSP);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm cần đánh giá.";
                return RedirectToAction("Index");
            }

            if (Session["MaKH"] == null)
            {
                TempData["Error"] = "Bạn cần đăng nhập để đánh giá sản phẩm.";
                return RedirectToAction("Detail", new { slug = product.Slug });
            }

            int maKH = Convert.ToInt32(Session["MaKH"]);

            bool daMuaSanPham = false;

            try
            {
                daMuaSanPham = db.Database.SqlQuery<int>(
                    @"SELECT COUNT(*)
                      FROM DONHANG DH
                      INNER JOIN CHITIET_DONHANG CT ON DH.MaDH = CT.MaDH
                      WHERE DH.MaKH = @p0
                      AND CT.MaSP = @p1
                      AND DH.TrangThai IN (N'Hoàn thành', N'Đã giao', N'Đã thanh toán')",
                    maKH,
                    MaSP
                ).FirstOrDefault() > 0;
            }
            catch
            {
                daMuaSanPham = false;
            }

            if (!daMuaSanPham)
            {
                TempData["Error"] = "Chỉ khách hàng đã mua sản phẩm này mới được đánh giá.";
                return RedirectToAction("Detail", new { slug = product.Slug });
            }

            bool daDanhGia = db.DANHGIAs.Any(x =>
                x.MaKH == maKH &&
                x.MaSP == MaSP &&
                x.TrangThai == true
            );

            if (daDanhGia)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction("Detail", new { slug = product.Slug });
            }

            if (SoSao < 1 || SoSao > 5)
            {
                TempData["Error"] = "Số sao đánh giá không hợp lệ.";
                return RedirectToAction("Detail", new { slug = product.Slug });
            }

            if (string.IsNullOrWhiteSpace(BinhLuan))
            {
                TempData["Error"] = "Vui lòng nhập nội dung bình luận.";
                return RedirectToAction("Detail", new { slug = product.Slug });
            }

            DANHGIA review = new DANHGIA
            {
                MaKH = maKH,
                MaSP = MaSP,
                SoSao = SoSao,
                NoiDung = BinhLuan.Trim(),
                NgayDanhGia = DateTime.Now,
                TrangThai = true
            };

            db.DANHGIAs.Add(review);
            db.SaveChanges();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm.";

            return RedirectToAction("Detail", new { slug = product.Slug });
        }

        [HttpPost]
        public ActionResult ToggleFavorite(int productId)
        {
            try
            {
                if (Session["MaKH"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        needLogin = true,
                        message = "Vui lòng đăng nhập để yêu thích sản phẩm."
                    });
                }

                int maKH = Convert.ToInt32(Session["MaKH"]);

                var product = db.SANPHAMs.Find(productId);

                if (product == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm."
                    });
                }

                int existed = db.Database.SqlQuery<int>(
                    @"SELECT COUNT(*) 
                      FROM YEUTHICH 
                      WHERE MaKH = @p0 AND MaSP = @p1",
                    maKH,
                    productId
                ).FirstOrDefault();

                bool isFavorite;

                if (existed > 0)
                {
                    db.Database.ExecuteSqlCommand(
                        @"DELETE FROM YEUTHICH 
                          WHERE MaKH = @p0 AND MaSP = @p1",
                        maKH,
                        productId
                    );

                    isFavorite = false;
                }
                else
                {
                    db.Database.ExecuteSqlCommand(
                        @"INSERT INTO YEUTHICH(MaKH, MaSP, NgayThem)
                          VALUES(@p0, @p1, GETDATE())",
                        maKH,
                        productId
                    );

                    isFavorite = true;
                }

                return Json(new
                {
                    success = true,
                    isFavorite = isFavorite,
                    message = isFavorite
                        ? "Đã thêm vào danh sách yêu thích."
                        : "Đã bỏ khỏi danh sách yêu thích."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi cập nhật yêu thích: " + ex.Message
                });
            }
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

    public class StoreProductOptionViewModel
    {
        public int MaTK_Store { get; set; }

        public string TenCuaHang { get; set; }

        public decimal GiaBan { get; set; }

        public int SoLuongTon { get; set; }
    }
}