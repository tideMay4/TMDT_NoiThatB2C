using DoAnCK.Models;
using DoAnCK.Services_Ai;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DoAnCK.Controllers
{
    public class HomeController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index()
        {
            var sanPhamMoi = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .Where(x => x.TrangThai == true)
                .OrderByDescending(x => x.NgayTao)
                .ThenByDescending(x => x.MaSP)
                .Take(6)
                .ToList();

            return View(sanPhamMoi);
        }

        // =========================================================
        // TÌM KIẾM THEO CÂU LỆNH TỰ NHIÊN
        // Ví dụ: "tôi muốn mua sofa cho phòng khách"
        // Không phụ thuộc Gemini API, không bị lỗi quota
        // =========================================================
        public ActionResult Search(string keyword)
        {
            ViewBag.TuKhoa = keyword;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                ViewBag.Message = "Vui lòng nhập từ khóa hoặc mô tả sản phẩm cần tìm.";
                return View(new List<SANPHAM>());
            }

            keyword = keyword.Trim();

            List<string> searchTerms = BuildSearchTerms(keyword);

            var products = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .Where(x => x.TrangThai == true)
                .ToList();

            var ketQua = products
                .Select(sp => new
                {
                    Product = sp,
                    Score = CalculateSearchScore(sp, searchTerms)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Product.NgayTao)
                .ThenByDescending(x => x.Product.MaSP)
                .Select(x => x.Product)
                .Take(12)
                .ToList();

            if (ketQua.Count == 0)
            {
                ViewBag.Message = "Không tìm thấy sản phẩm phù hợp với mô tả của bạn.";
            }

            return View(ketQua);
        }

        // =========================================================
        // TÌM KIẾM BẰNG HÌNH ẢNH
        // Vẫn cần Gemini API Key còn quota
        // Nếu Gemini lỗi, chỉ hiện thông báo ngắn gọn
        // =========================================================
        [HttpPost]
        public async Task<ActionResult> ImageSearch(System.Web.HttpPostedFileBase imageUpload)
        {
            if (imageUpload == null || imageUpload.ContentLength == 0)
            {
                TempData["Error"] = "Vui lòng chọn ảnh để tìm kiếm.";
                return RedirectToAction("Index");
            }

            try
            {
                byte[] uploadedBytes = new byte[imageUpload.ContentLength];

                imageUpload.InputStream.Position = 0;
                imageUpload.InputStream.Read(uploadedBytes, 0, imageUpload.ContentLength);

                string mimeType = imageUpload.ContentType;

                string imageDescription = await VisualSearchHelper.DescribeImageWithGeminiAsync(uploadedBytes, mimeType);

                if (string.IsNullOrWhiteSpace(imageDescription))
                {
                    ViewBag.Message = "Hệ thống AI xử lý ảnh đang bận, vui lòng thử lại sau hoặc dùng tìm kiếm bằng từ khóa.";
                    ViewBag.TuKhoa = "Tìm kiếm bằng hình ảnh";
                    return View("Search", new List<SANPHAM>());
                }

                if (imageDescription.StartsWith("ERROR"))
                {
                    ViewBag.Message = GetFriendlyAIErrorMessage(imageDescription);
                    ViewBag.TuKhoa = "Tìm kiếm bằng hình ảnh";
                    return View("Search", new List<SANPHAM>());
                }

                // Sau khi Gemini mô tả ảnh, dùng lại chức năng tìm kiếm câu tự nhiên
                List<string> searchTerms = BuildSearchTerms(imageDescription);

                var products = db.SANPHAMs
                    .Include(x => x.DANHMUC)
                    .Where(x => x.TrangThai == true)
                    .ToList();

                var ketQua = products
                    .Select(sp => new
                    {
                        Product = sp,
                        Score = CalculateSearchScore(sp, searchTerms)
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Product.NgayTao)
                    .ThenByDescending(x => x.Product.MaSP)
                    .Select(x => x.Product)
                    .Take(12)
                    .ToList();

                if (ketQua.Count == 0)
                {
                    ViewBag.Message = "AI hiểu ảnh của bạn là: \"" + imageDescription + "\". Nhưng chưa tìm thấy sản phẩm phù hợp trong kho.";
                }

                ViewBag.TuKhoa = "AI nhận diện ảnh: \"" + imageDescription + "\"";

                return View("Search", ketQua);
            }
            catch
            {
                ViewBag.Message = "Có lỗi khi tìm kiếm bằng hình ảnh. Vui lòng thử lại sau hoặc dùng tìm kiếm bằng từ khóa.";
                ViewBag.TuKhoa = "Tìm kiếm bằng hình ảnh";

                return View("Search", new List<SANPHAM>());
            }
        }

        // =========================================================
        // TÁCH CÂU NGƯỜI DÙNG THÀNH TỪ KHÓA QUAN TRỌNG
        // =========================================================
        private List<string> BuildSearchTerms(string input)
        {
            string normalized = NormalizeText(input);

            string[] stopWords =
            {
                "toi", "muon", "mua", "can", "tim", "kiem",
                "cho", "de", "mot", "cai", "chiec", "san", "pham",
                "hang", "noi", "that", "minh", "toi can", "toi muon"
            };

            foreach (var word in stopWords)
            {
                string w = NormalizeText(word);

                normalized = " " + normalized + " ";
                normalized = normalized.Replace(" " + w + " ", " ");
                normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            }

            List<string> terms = new List<string>();

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                terms.Add(normalized);
            }

            var words = normalized
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 2)
                .ToList();

            terms.AddRange(words);

            // =========================
            // Mapping theo loại sản phẩm
            // =========================

            if (normalized.Contains("sofa") ||
                normalized.Contains("ghe sofa") ||
                normalized.Contains("salon"))
            {
                terms.Add("sofa");
                terms.Add("ghe sofa");
                terms.Add("salon");
                terms.Add("phong khach");
            }

            if (normalized.Contains("ghe"))
            {
                terms.Add("ghe");
                terms.Add("ghe go");
                terms.Add("ghe sofa");
                terms.Add("ghe an");
            }

            if (normalized.Contains("ban"))
            {
                terms.Add("ban");
                terms.Add("ban tra");
                terms.Add("ban an");
                terms.Add("ban lam viec");
            }

            if (normalized.Contains("giuong"))
            {
                terms.Add("giuong");
                terms.Add("giuong ngu");
                terms.Add("phong ngu");
            }

            if (normalized.Contains("tu"))
            {
                terms.Add("tu");
                terms.Add("tu quan ao");
                terms.Add("tu go");
                terms.Add("ke tu");
            }

            if (normalized.Contains("ke"))
            {
                terms.Add("ke");
                terms.Add("ke tivi");
                terms.Add("ke sach");
                terms.Add("ke go");
            }

            if (normalized.Contains("guong") ||
                normalized.Contains("mirror"))
            {
                terms.Add("guong");
                terms.Add("mirror");
                terms.Add("guong trang tri");
            }

            if (normalized.Contains("den") ||
                normalized.Contains("lighting"))
            {
                terms.Add("den");
                terms.Add("den trang tri");
                terms.Add("lighting");
            }

            // =========================
            // Mapping theo không gian
            // =========================

            if (normalized.Contains("phong khach"))
            {
                terms.Add("phong khach");
                terms.Add("sofa");
                terms.Add("ghe sofa");
                terms.Add("ban tra");
                terms.Add("ke tivi");
                terms.Add("ghe");
            }

            if (normalized.Contains("phong ngu"))
            {
                terms.Add("phong ngu");
                terms.Add("giuong");
                terms.Add("tu quan ao");
                terms.Add("ban trang diem");
            }

            if (normalized.Contains("phong an") ||
                normalized.Contains("nha bep") ||
                normalized.Contains("bep"))
            {
                terms.Add("phong an");
                terms.Add("ban an");
                terms.Add("ghe an");
                terms.Add("tu bep");
            }

            if (normalized.Contains("phong lam viec") ||
                normalized.Contains("lam viec") ||
                normalized.Contains("hoc tap"))
            {
                terms.Add("ban lam viec");
                terms.Add("ghe lam viec");
                terms.Add("ke sach");
            }

            // =========================
            // Mapping màu sắc / chất liệu
            // =========================

            if (normalized.Contains("go"))
            {
                terms.Add("go");
                terms.Add("go tu nhien");
                terms.Add("go cong nghiep");
            }

            if (normalized.Contains("kem"))
            {
                terms.Add("kem");
                terms.Add("mau kem");
                terms.Add("be");
            }

            if (normalized.Contains("trang"))
            {
                terms.Add("trang");
                terms.Add("mau trang");
            }

            if (normalized.Contains("den"))
            {
                terms.Add("den");
                terms.Add("mau den");
            }

            if (normalized.Contains("nau"))
            {
                terms.Add("nau");
                terms.Add("mau nau");
                terms.Add("go");
            }

            if (normalized.Contains("xam"))
            {
                terms.Add("xam");
                terms.Add("mau xam");
            }

            // Loại trùng
            terms = terms
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => NormalizeText(x))
                .Where(x => x.Length >= 2)
                .Distinct()
                .ToList();

            return terms;
        }

        // =========================================================
        // CHẤM ĐIỂM SẢN PHẨM THEO TỪ KHÓA
        // =========================================================
        private int CalculateSearchScore(SANPHAM sp, List<string> terms)
        {
            int score = 0;

            string tenSP = NormalizeText(sp.TenSP);
            string moTa = NormalizeText(sp.MoTa);
            string thuongHieu = NormalizeText(sp.ThuongHieu);
            string danhMuc = sp.DANHMUC != null ? NormalizeText(sp.DANHMUC.TenDM) : "";
            string metaTitle = NormalizeText(sp.MetaTitle);
            string metaDescription = NormalizeText(sp.MetaDescription);
            string metaKeyword = NormalizeText(sp.MetaKeyword);

            foreach (var term in terms)
            {
                string t = NormalizeText(term);

                if (string.IsNullOrWhiteSpace(t))
                {
                    continue;
                }

                if (tenSP == t)
                {
                    score += 30;
                }
                else if (tenSP.Contains(t))
                {
                    score += 15;
                }

                if (danhMuc == t)
                {
                    score += 25;
                }
                else if (danhMuc.Contains(t))
                {
                    score += 12;
                }

                if (metaKeyword.Contains(t))
                {
                    score += 10;
                }

                if (metaTitle.Contains(t))
                {
                    score += 8;
                }

                if (metaDescription.Contains(t))
                {
                    score += 6;
                }

                if (moTa.Contains(t))
                {
                    score += 5;
                }

                if (thuongHieu.Contains(t))
                {
                    score += 3;
                }
            }

            return score;
        }

        // =========================================================
        // CHUẨN HÓA TIẾNG VIỆT: bỏ dấu, lowercase
        // =========================================================
        private string NormalizeText(string text)
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
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            string result = builder.ToString().Normalize(NormalizationForm.FormC);

            result = Regex.Replace(result, @"[^a-z0-9\s]", " ");
            result = Regex.Replace(result, @"\s+", " ").Trim();

            return result;
        }

        // =========================================================
        // ẨN LỖI JSON DÀI TỪ GEMINI
        // =========================================================
        private string GetFriendlyAIErrorMessage(string errorText)
        {
            if (string.IsNullOrWhiteSpace(errorText))
            {
                return "Hệ thống AI xử lý ảnh đang gặp lỗi. Vui lòng thử lại sau.";
            }

            string e = errorText.ToLower();

            if (e.Contains("429") ||
                e.Contains("quota") ||
                e.Contains("resource_exhausted"))
            {
                return "Tìm kiếm bằng hình ảnh đang tạm hết lượt AI. Vui lòng thử lại sau hoặc dùng tìm kiếm bằng từ khóa.";
            }

            if (e.Contains("404") ||
                e.Contains("not_found") ||
                e.Contains("no longer available"))
            {
                return "Model AI hiện tại không khả dụng. Vui lòng kiểm tra lại model trong VisualSearchHelper.cs.";
            }

            if (e.Contains("api key") ||
                e.Contains("unauthorized") ||
                e.Contains("invalid authentication"))
            {
                return "Gemini API Key chưa đúng hoặc chưa được cấu hình. Vui lòng kiểm tra lại API key.";
            }

            return "Hệ thống AI xử lý ảnh đang gặp lỗi. Vui lòng thử lại sau hoặc dùng tìm kiếm bằng từ khóa.";
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
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