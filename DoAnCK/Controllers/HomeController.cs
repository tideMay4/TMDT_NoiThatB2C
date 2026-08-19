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
using DoAnCK.Helpers;

namespace DoAnCK.Controllers
{
    public class HomeController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index()
        {
            var sanPhamNoiBat = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .Where(x => x.TrangThai == true && x.NoiBat == true)
                .OrderByDescending(x => x.NgayTao)
                .ThenByDescending(x => x.MaSP)
                .Take(8)
                .ToList();

            ViewBag.ComboSuggestions = ComboSuggestionHelper
                .GetInventoryComboSuggestions(db)
                .Take(4)
                .ToList();

            return View(sanPhamNoiBat);
        }

        // =========================================================
        // TÌM KIẾM THEO CÂU LỆNH TỰ NHIÊN
        // Ví dụ: "tôi muốn mua sofa cho phòng khách"
        // Không phụ thuộc Gemini API, không bị lỗi quota
        // =========================================================
        // =========================================================
        // TÌM KIẾM LAI (HYBRID SEARCH: AI VECTOR + KEYWORD)
        // =========================================================
        public async Task<ActionResult> Search(string keyword)
        {
            ViewBag.TuKhoa = keyword;

            var products = db.SANPHAMs.Include(x => x.DANHMUC).Where(x => x.TrangThai == true).ToList();

            // 1. Xử lý khi người dùng không nhập gì hoặc nhập quá ngắn
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length < 2)
            {
                ViewBag.Message = "Dưới đây là các sản phẩm nổi bật gợi ý cho bạn:";
                // Lấy 12 sản phẩm mới nhất làm gợi ý
                return View(products.OrderByDescending(x => x.NgayTao).Take(12).ToList());
            }

            keyword = keyword.Trim();

            // 2. Nạp dữ liệu Vector vào RAM
            await AIEmbeddingHelper.InitCacheAsync(db);
            float[] queryVector = await AIEmbeddingHelper.GetVectorFromTextAsync(keyword);

            List<SANPHAM> ketQua = new List<SANPHAM>();

            if (queryVector != null)
            {
                // 3. THUẬT TOÁN HYBRID: Kết hợp điểm AI và điểm Từ Khóa
                List<string> searchTerms = BuildSearchTerms(keyword);

                var aiResults = products.Select(sp => new
                {
                    Product = sp,
                    // Điểm ngữ nghĩa AI (0.0 -> 1.0)
                    AiScore = AIEmbeddingHelper.ProductVectorCache.ContainsKey(sp.MaSP)
                        ? AIEmbeddingHelper.CosineSimilarity(queryVector, AIEmbeddingHelper.ProductVectorCache[sp.MaSP])
                        : 0,
                    // Điểm trùng khớp từ khóa cũ
                    KeywordScore = CalculateSearchScore(sp, searchTerms)
                })
                // ĐIỀU KIỆN MỞ RỘNG: AI thấy hơi giống (>= 0.3) HOẶC chứa từ khóa quan trọng (>= 15)
                .Where(x => x.AiScore >= 0.3 || x.KeywordScore >= 15)
                .OrderByDescending(x => x.AiScore) // Ưu tiên xếp theo độ thông minh của AI trước
                .ThenByDescending(x => x.KeywordScore) // Sau đó mới xếp theo từ khóa
                .Select(x => x.Product)
                .Take(12)
                .ToList();

                ketQua = aiResults;
            }
            else
            {
                // 4. Nếu API Gemini quá tải, chỉ dùng từ khóa
                List<string> searchTerms = BuildSearchTerms(keyword);
                ketQua = products
                    .Select(sp => new { Product = sp, Score = CalculateSearchScore(sp, searchTerms) })
                    .Where(x => x.Score >= 15)
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Product.NgayTao)
                    .Select(x => x.Product)
                    .Take(12)
                    .ToList();

                ViewBag.Message = "Hệ thống AI đang bận, tạm thời dùng tìm kiếm từ khóa.";
            }

            // 5. CHỐNG TRANG TRẮNG: Nếu tìm mỏi mắt vẫn không có kết quả
            if (ketQua.Count == 0)
            {
                ViewBag.Message = "Không tìm thấy sản phẩm sát với yêu cầu. Nhưng bạn có thể tham khảo các gợi ý hấp dẫn dưới đây:";
                // Lấy 12 sản phẩm mới nhất bù vào
                ketQua = products.OrderByDescending(x => x.NgayTao).Take(12).ToList();
            }

            return View(ketQua);
        } // Thêm đoạn này vào bên trong class HomeController
        [HttpPost]
        public async Task<JsonResult> Chat(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return Json(new { reply = "Vui lòng nhập câu hỏi." });

            // 1. Trích xuất từ khóa đơn giản để lấy thông tin sản phẩm liên quan từ DB
            string searchKeyword = NormalizeText(message);
            string contextInfo = "Không tìm thấy thông tin cụ thể.";

            // Tìm thử 3 sản phẩm liên quan nhất dựa trên hàm tìm kiếm có sẵn
            var searchTerms = BuildSearchTerms(searchKeyword);
            if (searchTerms.Count > 0)
            {
                var products = db.SANPHAMs.Include(x => x.DANHMUC).Where(x => x.TrangThai == true).ToList();
                var ketQua = products
                    .Select(sp => new { Product = sp, Score = CalculateSearchScore(sp, searchTerms) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Take(3)
                    .ToList();

                if (ketQua.Any())
                {
                    var sb = new StringBuilder();
                    foreach (var item in ketQua)
                    {
                        sb.AppendLine($"- Tên SP: {item.Product.TenSP}, Giá: {item.Product.GiaHienTai:N0} VNĐ, Mô tả ngắn: {item.Product.MoTa}");
                    }
                    contextInfo = sb.ToString();
                }
            }

            // 2. Gửi cho Gemini xử lý kèm ngữ cảnh
            string aiReply = await ChatbotHelper.GetChatbotResponseAsync(message, contextInfo);

            return Json(new { reply = aiReply });
        }

        // =========================================================
        // TÌM KIẾM BẰNG HÌNH ẢNH
        // Vẫn cần Gemini API Key còn quota
        // Nếu Gemini lỗi, chỉ hiện thông báo ngắn gọn
        // =========================================================
        // =========================================================
        // TÌM KIẾM BẰNG HÌNH ẢNH (PHÂN LOẠI GIỐNG 100% VÀ TƯƠNG TỰ)
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

                // 1. Nhờ Gemini nhìn và mô tả bức ảnh
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

                ViewBag.TuKhoa = "AI nhận diện ảnh: \"" + imageDescription + "\"";

                // 2. Lấy toàn bộ sản phẩm từ DB
                var products = db.SANPHAMs.Include(x => x.DANHMUC).Where(x => x.TrangThai == true).ToList();

                // 3. Kết hợp AI Semantic (Vector) và Từ khóa để chấm điểm mô tả hình ảnh
                await AIEmbeddingHelper.InitCacheAsync(db);
                float[] queryVector = await AIEmbeddingHelper.GetVectorFromTextAsync(imageDescription);
                List<string> searchTerms = BuildSearchTerms(imageDescription);

                var danhSachChamDiem = products.Select(sp => new
                {
                    Product = sp,
                    AiScore = (queryVector != null && AIEmbeddingHelper.ProductVectorCache.ContainsKey(sp.MaSP))
                        ? AIEmbeddingHelper.CosineSimilarity(queryVector, AIEmbeddingHelper.ProductVectorCache[sp.MaSP])
                        : 0,
                    KeywordScore = CalculateSearchScore(sp, searchTerms)
                })
                .OrderByDescending(x => x.AiScore)
                .ThenByDescending(x => x.KeywordScore)
                .ToList();

                List<SANPHAM> ketQua = new List<SANPHAM>();

                if (danhSachChamDiem.Any())
                {
                    // Lấy ra ứng cử viên có điểm cao nhất (Thủ khoa)
                    var top1 = danhSachChamDiem.First();

                    // 4. KIỂM TRA ĐIỀU KIỆN "GIỐNG HỆT 100%"
                    // Đặt ngưỡng: AI thấy cực giống (>= 0.75) HOẶC trùng quá nhiều từ khóa (>= 40)
                    if (top1.AiScore >= 0.75 || top1.KeywordScore >= 40)
                    {
                        ViewBag.Message = "Đã tìm thấy sản phẩm chính xác 100% theo hình ảnh của bạn!";
                        // CHỈ LẤY ĐÚNG 1 SẢN PHẨM
                        ketQua.Add(top1.Product);
                    }
                    else
                    {
                        // 5. ĐIỀU KIỆN "TƯƠNG TỰ"
                        // Điểm không đủ cao để chắc chắn 100%, nên sẽ hiện một list gợi ý
                        ViewBag.Message = "Không có sản phẩm giống hệt 100%. Dưới đây là các sản phẩm tương tự để bạn tham khảo:";

                        ketQua = danhSachChamDiem
                            .Where(x => x.AiScore >= 0.25 || x.KeywordScore >= 10) // Hạ điểm để lấy đồ na ná nhau
                            .Select(x => x.Product)
                            .Take(8) // Hiển thị 8 sản phẩm tương tự
                            .ToList();
                    }
                }

                // 6. Xử lý khi trang trắng (ảnh tào lao, không có nội thất)
                if (ketQua.Count == 0)
                {
                    ViewBag.Message = "AI hiểu ảnh của bạn là: \"" + imageDescription + "\". Nhưng chưa tìm thấy sản phẩm nào liên quan trong kho. Dưới đây là gợi ý:";
                    // Tự động gợi ý hàng mới cho khách
                    ketQua = products.OrderByDescending(x => x.NgayTao).Take(8).ToList();
                }

                return View("Search", ketQua);
            }
            catch
            {
                ViewBag.Message = "Có lỗi khi tìm kiếm bằng hình ảnh. Vui lòng thử lại sau.";
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