using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using DoAnCK.Models;
using DoAnCK.Areas.Admin.Models;

namespace DoAnCK.Areas.Admin.Controllers
{
    public class ProductController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index()
        {
            var products = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .OrderByDescending(x => x.MaSP)
                .ToList()
                .Select(x => new ProductViewModel
                {
                    MaSP = x.MaSP,
                    TenSP = x.TenSP,
                    MaDM = x.MaDM,
                    DanhMuc = x.DANHMUC != null ? x.DANHMUC.TenDM : "Chưa phân loại",
                    HinhAnh = string.IsNullOrEmpty(x.HinhAnh)
                        ? "~/Content/images/no-image.jpg"
                        : x.HinhAnh,
                    Gia = x.GiaHienTai,
                    GiaCu = null,
                    TonKho = x.SoLuongTon,
                    Slug = x.Slug,
                    MoTa = x.MoTa,
                    ThuongHieu = x.ThuongHieu,
                    BaoHanh = x.BaoHanh,
                    VAT = x.VAT,
                    MetaTitle = x.MetaTitle,
                    MetaDescription = x.MetaDescription,
                    MetaKeyword = x.MetaKeyword,
                    TrangThai = x.TrangThai,
                    NoiBat = x.NoiBat,
                    NgayTao = x.NgayTao
                })
                .ToList();

            var categories = db.DANHMUCs
                .Where(x => x.TrangThai == true)
                .OrderBy(x => x.TenDM)
                .ToList()
                .Select(x => new CategoryViewModel
                {
                    MaDM = x.MaDM,
                    TenDM = x.TenDM,
                    Slug = x.Slug,
                    MoTa = x.MoTa,
                    HinhAnh = x.HinhAnh,
                    SoSanPham = x.SANPHAMs.Count,
                    TrangThai = x.TrangThai,
                    NgayTao = x.NgayTao
                })
                .ToList();

            var priceHistories = db.GIAs
                .Include(x => x.SANPHAM)
                .OrderByDescending(x => x.NgayTao)
                .Take(20)
                .ToList()
                .Select(x => new ProductPriceViewModel
                {
                    MaGia = x.MaGia,
                    MaSP = x.MaSP,
                    TenSP = x.SANPHAM != null ? x.SANPHAM.TenSP : "Không xác định",
                    HinhAnh = x.SANPHAM != null && !string.IsNullOrEmpty(x.SANPHAM.HinhAnh)
                        ? x.SANPHAM.HinhAnh
                        : "~/Content/images/no-image.jpg",
                    Slug = x.SANPHAM != null ? x.SANPHAM.Slug : "",
                    GiaCu = x.GiaCu,
                    GiaMoi = x.GiaMoi,
                    NgayBatDau = x.NgayBatDau,
                    NgayKetThuc = x.NgayKetThuc,
                    LyDoThayDoi = x.LyDoThayDoi,
                    TrangThai = x.TrangThai,
                    NgayTao = x.NgayTao
                })
                .ToList();

            ViewBag.TotalProducts = products.Count;
            ViewBag.VisibleProducts = products.Count(x => x.TrangThai);
            ViewBag.LowStockProducts = products.Count(x => x.TonKho > 0 && x.TonKho <= 10);
            ViewBag.HiddenProducts = products.Count(x => !x.TrangThai);

            ViewBag.Categories = categories;
            ViewBag.PriceHistories = priceHistories;

            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            string TenSP,
            int MaDM,
            decimal GiaHienTai,
            int SoLuongTon,
            string Slug,
            string MoTa,
            string ThuongHieu,
            string BaoHanh,
            decimal? VAT,
            string MetaTitle,
            string MetaDescription,
            string MetaKeyword,
            bool TrangThai,
            bool NoiBat,
            HttpPostedFileBase HinhAnhFile)
        {
            if (string.IsNullOrWhiteSpace(TenSP))
            {
                TempData["Error"] = "Vui lòng nhập tên sản phẩm.";
                return RedirectToAction("Index");
            }

            if (MaDM <= 0)
            {
                TempData["Error"] = "Vui lòng chọn danh mục sản phẩm.";
                return RedirectToAction("Index");
            }

            var danhMuc = db.DANHMUCs.Find(MaDM);
            string tenDanhMuc = danhMuc != null ? danhMuc.TenDM : "";

            if (string.IsNullOrWhiteSpace(Slug))
            {
                Slug = TaoSlug(TenSP);
            }
            else
            {
                Slug = TaoSlug(Slug);
            }

            bool slugDaTonTai = db.SANPHAMs.Any(x => x.Slug == Slug);

            if (slugDaTonTai)
            {
                Slug = Slug + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            if (string.IsNullOrWhiteSpace(MetaTitle))
            {
                MetaTitle = TaoMetaTitle(TenSP, tenDanhMuc, ThuongHieu);
            }

            if (string.IsNullOrWhiteSpace(MetaKeyword))
            {
                MetaKeyword = TaoMetaKeyword(TenSP, tenDanhMuc);
            }

            if (string.IsNullOrWhiteSpace(MetaDescription))
            {
                MetaDescription = TaoMetaDescription(TenSP, tenDanhMuc, MoTa);
            }

            string hinhAnhPath = "~/Content/images/no-image.jpg";

            if (HinhAnhFile != null && HinhAnhFile.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/Content/images/products");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = Path.GetFileName(HinhAnhFile.FileName);
                string extension = Path.GetExtension(fileName);

                string newFileName = TaoSlug(Path.GetFileNameWithoutExtension(fileName))
                                     + "-"
                                     + DateTime.Now.ToString("yyyyMMddHHmmss")
                                     + extension;

                string savePath = Path.Combine(folderPath, newFileName);

                HinhAnhFile.SaveAs(savePath);

                hinhAnhPath = "~/Content/images/products/" + newFileName;
            }

            SANPHAM sanPham = new SANPHAM
            {
                TenSP = TenSP.Trim(),
                MaDM = MaDM,
                MoTa = MoTa,
                GiaHienTai = GiaHienTai,
                SoLuongTon = SoLuongTon,
                HinhAnh = hinhAnhPath,
                ThuongHieu = ThuongHieu,
                BaoHanh = BaoHanh,
                VAT = VAT ?? 0,
                Slug = Slug,
                MetaTitle = MetaTitle,
                MetaDescription = MetaDescription,
                MetaKeyword = MetaKeyword,
                NoiBat = NoiBat,
                TrangThai = TrangThai,
                NgayTao = DateTime.Now
            };

            db.SANPHAMs.Add(sanPham);
            db.SaveChanges();

            /*
                Sau khi thêm sản phẩm vào SANPHAM,
                tự động gán sản phẩm này cho Store 2 và Store 3.
                Dùng SQL trực tiếp để không phụ thuộc EDMX có entity CUAHANG_SANPHAM hay chưa.
            */

            db.Database.ExecuteSqlCommand(
                @"INSERT INTO CUAHANG_SANPHAM
      (
          MaTK_Store,
          MaSP,
          GiaBan,
          SoLuongTon,
          TrangThai,
          NgayCapNhat
      )
      SELECT 
          CH.MaTK,
          @p0,
          CASE 
              WHEN CH.MaTK = 3 THEN @p1 * 0.95
              ELSE @p1
          END,
          CASE 
              WHEN CH.MaTK = 2 THEN @p2 + 10
              ELSE 
                  CASE 
                      WHEN @p2 - 3 < 0 THEN 0
                      ELSE @p2 - 3
                  END
          END,
          1,
          GETDATE()
      FROM CUAHANG CH
      WHERE CH.MaTK IN (2, 3)
        AND NOT EXISTS (
            SELECT 1
            FROM CUAHANG_SANPHAM CSP
            WHERE CSP.MaTK_Store = CH.MaTK
              AND CSP.MaSP = @p0
        )",
                sanPham.MaSP,
                sanPham.GiaHienTai,
                sanPham.SoLuongTon
            );

            TempData["Success"] = "Thêm sản phẩm thành công.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePrice(int MaSP, decimal GiaMoi, DateTime NgayBatDau, DateTime? NgayKetThuc, string LyDoThayDoi)
        {
            var product = db.SANPHAMs.Find(MaSP);

            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm cần cập nhật giá.";
                return RedirectToAction("Index");
            }

            if (GiaMoi <= 0)
            {
                TempData["Error"] = "Giá mới phải lớn hơn 0.";
                return RedirectToAction("Index");
            }

            if (NgayKetThuc.HasValue && NgayKetThuc.Value.Date < NgayBatDau.Date)
            {
                TempData["Error"] = "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.";
                return RedirectToAction("Index");
            }

            DateTime today = DateTime.Today;

            /*
                Kiểm tra sản phẩm có giá đang áp dụng chưa hết hạn hay không.
                Nếu còn giá đang áp dụng thì không cho thay đổi giá mới.
            */
            var giaDangApDung = db.GIAs
                .Where(x =>
                    x.MaSP == MaSP &&
                    x.TrangThai == "Đang áp dụng" &&
                    x.NgayBatDau <= today &&
                    (
                        x.NgayKetThuc == null ||
                        x.NgayKetThuc >= today
                    )
                )
                .OrderByDescending(x => x.NgayBatDau)
                .FirstOrDefault();

            if (giaDangApDung != null)
            {
                string ngayHetHanText = giaDangApDung.NgayKetThuc.HasValue
                    ? giaDangApDung.NgayKetThuc.Value.ToString("dd/MM/yyyy")
                    : "chưa có ngày kết thúc";

                TempData["Error"] =
                    "Không thể thay đổi giá sản phẩm \"" + product.TenSP + "\" vì đang có giá áp dụng chưa hết hạn. " +
                    "Giá hiện tại còn hiệu lực đến: " + ngayHetHanText + ".";

                return RedirectToAction("Index");
            }

            decimal giaCu = product.GiaHienTai;

            GIA giaMoi = new GIA
            {
                MaSP = MaSP,
                GiaCu = giaCu,
                GiaMoi = GiaMoi,
                NgayBatDau = NgayBatDau.Date,
                NgayKetThuc = NgayKetThuc.HasValue ? NgayKetThuc.Value.Date : (DateTime?)null,
                LyDoThayDoi = LyDoThayDoi,
                TrangThai = "Đang áp dụng",
                NgayTao = DateTime.Now
            };

            db.GIAs.Add(giaMoi);

            /*
                Nếu ngày bắt đầu nhỏ hơn hoặc bằng hôm nay,
                cập nhật luôn giá hiện tại của sản phẩm.
            */
            if (NgayBatDau.Date <= today)
            {
                product.GiaHienTai = GiaMoi;
            }

            db.SaveChanges();

            TempData["Success"] = "Cập nhật giá sản phẩm thành công.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(int MaSP, int SoSao, string BinhLuan)
        {
            int? maKH = null;

            if (Session["MaKH"] != null)
            {
                maKH = Convert.ToInt32(Session["MaKH"]);
            }

            if (maKH == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để gửi đánh giá.";
                return RedirectToAction("Login", "Account");
            }

            var product = db.SANPHAMs.Find(MaSP);

            if (product == null)
            {
                return HttpNotFound();
            }

            if (SoSao < 1 || SoSao > 5)
            {
                TempData["Error"] = "Số sao đánh giá không hợp lệ.";
                return RedirectToAction("Detail", new { slug = product.Slug });
            }

            if (string.IsNullOrWhiteSpace(BinhLuan))
            {
                TempData["Error"] = "Vui lòng nhập nội dung đánh giá.";
                return RedirectToAction("Detail", new { slug = product.Slug });
            }

            bool daDanhGia = db.DANHGIAs.Any(x =>
                x.MaSP == MaSP &&
                x.MaKH == maKH.Value
            );

            if (daDanhGia)
            {
                TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction("Detail", new { slug = product.Slug });
            }

            DANHGIA danhGia = new DANHGIA
            {
                MaSP = MaSP,
                MaKH = maKH.Value,
                SoSao = SoSao,
                NoiDung = BinhLuan.Trim(),
                NgayDanhGia = DateTime.Now,
                TrangThai = true
            };

            db.DANHGIAs.Add(danhGia);
            db.SaveChanges();

            TempData["Success"] = "Gửi đánh giá thành công.";

            return RedirectToAction("Detail", new { slug = product.Slug });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int MaSP,
            string TenSP,
            int MaDM,
            decimal GiaHienTai,
            int SoLuongTon,
            string Slug,
            string MoTa,
            string ThuongHieu,
            string BaoHanh,
            decimal? VAT,
            string MetaTitle,
            string MetaDescription,
            string MetaKeyword,
            bool TrangThai,
            bool NoiBat,
            HttpPostedFileBase HinhAnhFile)
        {
            var sanPham = db.SANPHAMs.Find(MaSP);

            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm cần sửa.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(TenSP))
            {
                TempData["Error"] = "Tên sản phẩm không được để trống.";
                return RedirectToAction("Index");
            }

            if (MaDM <= 0)
            {
                TempData["Error"] = "Vui lòng chọn danh mục sản phẩm.";
                return RedirectToAction("Index");
            }

            var danhMuc = db.DANHMUCs.Find(MaDM);
            string tenDanhMuc = danhMuc != null ? danhMuc.TenDM : "";

            if (string.IsNullOrWhiteSpace(Slug))
            {
                Slug = TaoSlug(TenSP);
            }
            else
            {
                Slug = TaoSlug(Slug);
            }

            bool slugDaTonTai = db.SANPHAMs.Any(x => x.Slug == Slug && x.MaSP != MaSP);

            if (slugDaTonTai)
            {
                Slug = Slug + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            if (string.IsNullOrWhiteSpace(MetaTitle))
            {
                MetaTitle = TaoMetaTitle(TenSP, tenDanhMuc, ThuongHieu);
            }

            if (string.IsNullOrWhiteSpace(MetaKeyword))
            {
                MetaKeyword = TaoMetaKeyword(TenSP, tenDanhMuc);
            }

            if (string.IsNullOrWhiteSpace(MetaDescription))
            {
                MetaDescription = TaoMetaDescription(TenSP, tenDanhMuc, MoTa);
            }

            if (HinhAnhFile != null && HinhAnhFile.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/Content/images/products");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = Path.GetFileName(HinhAnhFile.FileName);
                string extension = Path.GetExtension(fileName);

                string newFileName = TaoSlug(Path.GetFileNameWithoutExtension(fileName))
                                     + "-"
                                     + DateTime.Now.ToString("yyyyMMddHHmmss")
                                     + extension;

                string savePath = Path.Combine(folderPath, newFileName);

                HinhAnhFile.SaveAs(savePath);

                sanPham.HinhAnh = "~/Content/images/products/" + newFileName;
            }

            sanPham.TenSP = TenSP.Trim();
            sanPham.MaDM = MaDM;
            sanPham.MoTa = MoTa;
            sanPham.GiaHienTai = GiaHienTai;
            sanPham.SoLuongTon = SoLuongTon;
            sanPham.ThuongHieu = ThuongHieu;
            sanPham.BaoHanh = BaoHanh;
            sanPham.VAT = VAT ?? 0;
            sanPham.Slug = Slug;
            sanPham.MetaTitle = MetaTitle;
            sanPham.MetaDescription = MetaDescription;
            sanPham.MetaKeyword = MetaKeyword;
            sanPham.NoiBat = NoiBat;
            sanPham.TrangThai = TrangThai;

            db.SaveChanges();

            TempData["Success"] = "Cập nhật sản phẩm thành công.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int MaSP)
        {
            var sanPham = db.SANPHAMs.Find(MaSP);

            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm cần xóa.";
                return RedirectToAction("Index");
            }

            try
            {
                var giaList = db.GIAs.Where(x => x.MaSP == MaSP).ToList();

                db.GIAs.RemoveRange(giaList);

                db.SANPHAMs.Remove(sanPham);

                db.SaveChanges();

                TempData["Success"] = "Xóa sản phẩm thành công.";
            }
            catch
            {
                TempData["Error"] = "Không thể xóa sản phẩm đã phát sinh giỏ hàng, đơn hàng hoặc đánh giá. Bạn nên chuyển sản phẩm sang trạng thái Đang ẩn.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPrice(
    int MaGia,
    int MaSP,
    decimal GiaMoi,
    DateTime NgayBatDau,
    DateTime? NgayKetThuc,
    string LyDoThayDoi,
    string TrangThai)
        {
            var gia = db.GIAs.Find(MaGia);

            if (gia == null)
            {
                TempData["Error"] = "Không tìm thấy lịch sử giá cần sửa.";
                return RedirectToAction("Index");
            }

            var sanPham = db.SANPHAMs.Find(MaSP);

            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index");
            }

            if (GiaMoi <= 0)
            {
                TempData["Error"] = "Giá mới phải lớn hơn 0.";
                return RedirectToAction("Index");
            }

            if (NgayKetThuc.HasValue && NgayKetThuc.Value < NgayBatDau)
            {
                TempData["Error"] = "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.";
                return RedirectToAction("Index");
            }

            if (TrangThai == "Đang áp dụng")
            {
                var giaDangApDung = db.GIAs
                    .Where(x => x.MaSP == MaSP && x.MaGia != MaGia && x.TrangThai == "Đang áp dụng")
                    .ToList();

                foreach (var item in giaDangApDung)
                {
                    item.TrangThai = "Ngừng áp dụng";
                }

                sanPham.GiaHienTai = GiaMoi;
            }

            gia.MaSP = MaSP;
            gia.GiaMoi = GiaMoi;
            gia.NgayBatDau = NgayBatDau;
            gia.NgayKetThuc = NgayKetThuc;
            gia.LyDoThayDoi = LyDoThayDoi;
            gia.TrangThai = TrangThai;

            db.SaveChanges();

            TempData["Success"] = "Cập nhật lịch sử giá thành công.";

            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePrice(int MaGia)
        {
            var gia = db.GIAs.Find(MaGia);

            if (gia == null)
            {
                TempData["Error"] = "Không tìm thấy lịch sử giá cần xóa.";
                return RedirectToAction("Index");
            }

            var sanPham = db.SANPHAMs.Find(gia.MaSP);

            if (sanPham != null && gia.TrangThai == "Đang áp dụng")
            {
                sanPham.GiaHienTai = gia.GiaCu;
            }

            db.GIAs.Remove(gia);
            db.SaveChanges();

            TempData["Success"] = "Xóa lịch sử giá thành công.";

            return RedirectToAction("Index");
        }
        private string TaoMetaTitle(string tenSP, string tenDanhMuc, string thuongHieu)
        {
            string brand = string.IsNullOrWhiteSpace(thuongHieu) ? "MODERNO" : thuongHieu.Trim();

            string result;

            if (!string.IsNullOrWhiteSpace(tenDanhMuc))
            {
                result = tenSP.Trim() + " - " + tenDanhMuc.Trim() + " hiện đại | " + brand;
            }
            else
            {
                result = tenSP.Trim() + " | " + brand;
            }

            return CatChuoi(result, 255);
        }

        private string TaoMetaKeyword(string tenSP, string tenDanhMuc)
        {
            string result = tenSP.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(tenDanhMuc))
            {
                result += ", " + tenDanhMuc.Trim().ToLower();
            }

            result += ", nội thất, nội thất hiện đại, moderno";

            return CatChuoi(result, 255);
        }

        private string TaoMetaDescription(string tenSP, string tenDanhMuc, string moTa)
        {
            string result;

            if (!string.IsNullOrWhiteSpace(moTa))
            {
                result = moTa.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(tenDanhMuc))
            {
                result = "Khám phá " + tenSP.Trim() + " thuộc danh mục " + tenDanhMuc.Trim() +
                         " tại MODERNO, phù hợp cho không gian sống hiện đại, tiện nghi và sang trọng.";
            }
            else
            {
                result = "Khám phá " + tenSP.Trim() +
                         " tại MODERNO, sản phẩm nội thất hiện đại, phù hợp cho không gian sống tiện nghi và sang trọng.";
            }

            return CatChuoi(result, 500);
        }

        private string CatChuoi(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            text = text.Trim();

            if (text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength - 3) + "...";
        }

        private string TaoSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            text = text.ToLower().Trim();
            text = text.Replace("đ", "d");

            string normalized = text.Normalize(NormalizationForm.FormD);

            StringBuilder builder = new StringBuilder();

            foreach (char c in normalized)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);

                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            text = builder.ToString().Normalize(NormalizationForm.FormC);

            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-");
            text = Regex.Replace(text, @"-+", "-");
            text = text.Trim('-');

            return text;
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