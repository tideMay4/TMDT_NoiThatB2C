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
                TenSP = TenSP,
                MaDM = MaDM,
                MoTa = MoTa,
                GiaHienTai = GiaHienTai,
                SoLuongTon = SoLuongTon,
                HinhAnh = hinhAnhPath,
                ThuongHieu = ThuongHieu,
                BaoHanh = BaoHanh,
                VAT = VAT ?? 0,
                Slug = Slug,
                MetaTitle = string.IsNullOrWhiteSpace(MetaTitle) ? TenSP : MetaTitle,
                MetaDescription = MetaDescription,
                MetaKeyword = MetaKeyword,
                NoiBat = NoiBat,
                TrangThai = TrangThai,
                NgayTao = DateTime.Now
            };

            db.SANPHAMs.Add(sanPham);
            db.SaveChanges();

            TempData["Success"] = "Thêm sản phẩm thành công.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePrice(
            int MaSP,
            decimal GiaMoi,
            DateTime NgayBatDau,
            DateTime? NgayKetThuc,
            string LyDoThayDoi,
            string TrangThai)
        {
            var sanPham = db.SANPHAMs.Find(MaSP);

            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm cần cập nhật giá.";
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

            decimal giaCu = sanPham.GiaHienTai;

            if (TrangThai == "Đang áp dụng")
            {
                var giaDangApDung = db.GIAs
                    .Where(x => x.MaSP == MaSP && x.TrangThai == "Đang áp dụng")
                    .ToList();

                foreach (var item in giaDangApDung)
                {
                    item.TrangThai = "Ngừng áp dụng";
                }

                sanPham.GiaHienTai = GiaMoi;
            }

            GIA gia = new GIA
            {
                MaSP = MaSP,
                GiaCu = giaCu,
                GiaMoi = GiaMoi,
                NgayBatDau = NgayBatDau,
                NgayKetThuc = NgayKetThuc,
                LyDoThayDoi = LyDoThayDoi,
                TrangThai = TrangThai,
                NgayTao = DateTime.Now
            };

            db.GIAs.Add(gia);
            db.SaveChanges();

            TempData["Success"] = "Cập nhật giá sản phẩm thành công.";

            return RedirectToAction("Index");
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

            sanPham.TenSP = TenSP;
            sanPham.MaDM = MaDM;
            sanPham.MoTa = MoTa;
            sanPham.GiaHienTai = GiaHienTai;
            sanPham.SoLuongTon = SoLuongTon;
            sanPham.ThuongHieu = ThuongHieu;
            sanPham.BaoHanh = BaoHanh;
            sanPham.VAT = VAT ?? 0;
            sanPham.Slug = Slug;
            sanPham.MetaTitle = string.IsNullOrWhiteSpace(MetaTitle) ? TenSP : MetaTitle;
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