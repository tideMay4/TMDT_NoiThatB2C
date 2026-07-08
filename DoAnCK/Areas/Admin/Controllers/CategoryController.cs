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
    public class CategoryController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index()
        {
            var categories = db.DANHMUCs
                .Include(x => x.SANPHAMs)
                .OrderByDescending(x => x.MaDM)
                .ToList()
                .Select(x => new CategoryViewModel
                {
                    MaDM = x.MaDM,
                    TenDM = x.TenDM,
                    Slug = x.Slug,
                    MoTa = x.MoTa,
                    HinhAnh = string.IsNullOrEmpty(x.HinhAnh)
                        ? "~/Content/images/no-image.jpg"
                        : x.HinhAnh,
                    SoSanPham = x.SANPHAMs.Count,
                    TrangThai = x.TrangThai,
                    NgayTao = x.NgayTao
                })
                .ToList();

            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string TenDM, string Slug, string MoTa, bool TrangThai, HttpPostedFileBase HinhAnhFile)
        {
            if (string.IsNullOrWhiteSpace(TenDM))
            {
                TempData["Error"] = "Vui lòng nhập tên danh mục.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(Slug))
            {
                Slug = TaoSlug(TenDM);
            }
            else
            {
                Slug = TaoSlug(Slug);
            }

            bool slugDaTonTai = db.DANHMUCs.Any(x => x.Slug == Slug);

            if (slugDaTonTai)
            {
                Slug = Slug + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            string hinhAnhPath = "~/Content/images/no-image.jpg";

            if (HinhAnhFile != null && HinhAnhFile.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/Content/images/categories");

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

                hinhAnhPath = "~/Content/images/categories/" + newFileName;
            }

            DANHMUC danhMuc = new DANHMUC
            {
                TenDM = TenDM,
                Slug = Slug,
                MoTa = MoTa,
                HinhAnh = hinhAnhPath,
                MetaTitle = TenDM,
                MetaDescription = MoTa,
                MetaKeyword = TenDM,
                TrangThai = TrangThai,
                NgayTao = DateTime.Now
            };

            db.DANHMUCs.Add(danhMuc);
            db.SaveChanges();

            TempData["Success"] = "Thêm danh mục thành công.";

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int MaDM, string TenDM, string Slug, string MoTa, bool TrangThai, HttpPostedFileBase HinhAnhFile)
        {
            var danhMuc = db.DANHMUCs.Find(MaDM);

            if (danhMuc == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục cần sửa.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(TenDM))
            {
                TempData["Error"] = "Tên danh mục không được để trống.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(Slug))
            {
                Slug = TaoSlug(TenDM);
            }
            else
            {
                Slug = TaoSlug(Slug);
            }

            bool slugDaTonTai = db.DANHMUCs.Any(x => x.Slug == Slug && x.MaDM != MaDM);

            if (slugDaTonTai)
            {
                Slug = Slug + "-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }

            if (HinhAnhFile != null && HinhAnhFile.ContentLength > 0)
            {
                string folderPath = Server.MapPath("~/Content/images/categories");

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

                danhMuc.HinhAnh = "~/Content/images/categories/" + newFileName;
            }

            danhMuc.TenDM = TenDM;
            danhMuc.Slug = Slug;
            danhMuc.MoTa = MoTa;
            danhMuc.MetaTitle = TenDM;
            danhMuc.MetaDescription = MoTa;
            danhMuc.MetaKeyword = TenDM;
            danhMuc.TrangThai = TrangThai;

            db.SaveChanges();

            TempData["Success"] = "Cập nhật danh mục thành công.";

            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int MaDM)
        {
            var danhMuc = db.DANHMUCs.Find(MaDM);

            if (danhMuc == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục cần xóa.";
                return RedirectToAction("Index");
            }

            bool coSanPham = db.SANPHAMs.Any(x => x.MaDM == MaDM);

            if (coSanPham)
            {
                TempData["Error"] = "Không thể xóa danh mục đang có sản phẩm. Bạn có thể chuyển danh mục sang trạng thái Đang ẩn.";
                return RedirectToAction("Index");
            }

            db.DANHMUCs.Remove(danhMuc);
            db.SaveChanges();

            TempData["Success"] = "Xóa danh mục thành công.";

            return RedirectToAction("Index");
        }
    }
}