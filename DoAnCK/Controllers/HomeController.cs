using DoAnCK.Models;
using DoAnCK.Services_Ai;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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
        // Hàm nhận từ khóa từ ô tìm kiếm
        // Hàm xử lý tìm kiếm bằng AI chuyên sâu
        // Hàm xử lý tìm kiếm Lai (Hybrid Search: Từ khóa + AI)
        public async Task<ActionResult> Search(string keyword)
        {
            ViewBag.TuKhoa = keyword;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return RedirectToAction("Index");
            }

            // =========================================================
            // BƯỚC 1: TÌM KIẾM TỪ KHÓA TRUYỀN THỐNG (SQL LIKE)
            // =========================================================
            string keywordLower = keyword.ToLower();

            // Tìm các sản phẩm có chứa từ khóa trong Tên, Mô tả hoặc Thương hiệu
            var ketQuaTruyenThong = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .Where(x => x.TrangThai == true &&
                           (x.TenSP.ToLower().Contains(keywordLower) ||
                            x.ThuongHieu.ToLower().Contains(keywordLower)))
                .ToList();

            // =========================================================
            // BƯỚC 2: TÌM KIẾM MỞ RỘNG BẰNG AI (VECTOR SEARCH)
            // =========================================================
            await AIEmbeddingHelper.InitCacheAsync(db);
            var queryVector = await AIEmbeddingHelper.GetVectorFromTextAsync(keyword);

            var ketQuaAI = new List<SANPHAM>();

            if (queryVector != null)
            {
                var danhSachDiem = new List<Tuple<int, double>>();
                foreach (var item in AIEmbeddingHelper.ProductVectorCache)
                {
                    double score = AIEmbeddingHelper.CosineSimilarity(queryVector, item.Value);
                    danhSachDiem.Add(new Tuple<int, double>(item.Key, score));
                }

                // Lọc các sản phẩm AI thấy khớp
                var topProductIds = danhSachDiem
                    .Where(t => t.Item2 >= 0.6)
                    .OrderByDescending(t => t.Item2)
                    .Select(t => t.Item1)
                    .Take(6)
                    .ToList();

                ketQuaAI = db.SANPHAMs
                    .Include(x => x.DANHMUC)
                    .Where(x => topProductIds.Contains(x.MaSP))
                    .ToList()
                    .OrderBy(x => topProductIds.IndexOf(x.MaSP)) // Sắp xếp theo điểm AI
                    .ToList();
            }

            // =========================================================
            // BƯỚC 3: GỘP KẾT QUẢ VÀ TRẢ VỀ VIEW
            // =========================================================

            // Gộp 2 danh sách lại, dùng GroupBy để loại bỏ những sản phẩm bị trùng 
            // (vừa tìm thấy bằng từ khóa, vừa được AI gọi tên)
            // Kết hợp ưu tiên: lấy toàn bộ từ khóa trước, sau đó mới lấy thêm từ AI
            var ketQuaCuoiCung = ketQuaTruyenThong
                .Concat(ketQuaAI)
                .GroupBy(x => x.MaSP)
                .Select(g => g.First())
                .OrderByDescending(x => ketQuaTruyenThong.Any(k => k.MaSP == x.MaSP) ? 1 : 0) // Ưu tiên kết quả từ khóa
                .Take(8)
                .ToList();

            // Xử lý thông báo lỗi nếu AI sập và từ khóa cũng không có
            if (queryVector == null && !ketQuaTruyenThong.Any())
            {
                ViewBag.Message = "Hệ thống AI đang bận, và không tìm thấy từ khóa khớp chính xác.";
            }

            return View(ketQuaCuoiCung);
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
        public async Task<ActionResult> ImageSearch(System.Web.HttpPostedFileBase imageUpload)
        {
            if (imageUpload == null || imageUpload.ContentLength == 0)
            {
                return RedirectToAction("Index");
            }

            byte[] uploadedBytes = new byte[imageUpload.ContentLength];

            // --- FIX LỖI CHÍNH: Bắt buộc phải reset vị trí đọc file về 0 ---
            imageUpload.InputStream.Position = 0;
            // ---------------------------------------------------------------

            imageUpload.InputStream.Read(uploadedBytes, 0, imageUpload.ContentLength);

            // 1. Nhờ Gemini nhìn ảnh
            string imageDescription = await VisualSearchHelper.DescribeImageWithGeminiAsync(uploadedBytes);

            // Bắt lỗi nếu AI trả về thông báo lỗi
            if (imageDescription != null && imageDescription.StartsWith("ERROR"))
            {
                ViewBag.Message = $"LỖI KỸ THUẬT TỪ AI: {imageDescription}";
                return View("Search", new List<SANPHAM>());
            }

            if (string.IsNullOrEmpty(imageDescription))
            {
                ViewBag.Message = "Hệ thống AI xử lý ảnh đang bận, vui lòng thử lại sau.";
                return View("Search", new List<SANPHAM>());
            }

            // 2. Chuyển chữ thành Vector
            await AIEmbeddingHelper.InitCacheAsync(db);
            var queryVector = await AIEmbeddingHelper.GetVectorFromTextAsync(imageDescription);

            if (queryVector == null)
            {
                ViewBag.Message = "Hệ thống tính toán Vector đang bận, vui lòng thử lại sau.";
                return View("Search", new List<SANPHAM>());
            }

            // 3. So sánh Vector ảnh với kho Vector chữ trong RAM
            var danhSachDiem = new List<Tuple<int, double>>();
            foreach (var item in AIEmbeddingHelper.ProductVectorCache)
            {
                double score = AIEmbeddingHelper.CosineSimilarity(queryVector, item.Value);
                danhSachDiem.Add(new Tuple<int, double>(item.Key, score));
            }

            // Hạ ngưỡng xuống 0.3 để đảm bảo AI miêu tả hơi lệch vẫn ra sản phẩm
            var topProductIds = danhSachDiem
                .Where(t => t.Item2 >= 0.50)
                .OrderByDescending(t => t.Item2)
                .Select(t => t.Item1)
                .Take(6)
                .ToList();

            var ketQua = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .Where(x => topProductIds.Contains(x.MaSP))
                .ToList()
                .OrderBy(x => topProductIds.IndexOf(x.MaSP))
                .ToList();

            // 4. Trả kết quả ra View
            if (!ketQua.Any())
            {
                ViewBag.Message = $"AI hiểu ảnh của bạn là: '{imageDescription}'. Nhưng trong kho không có sản phẩm nào khớp với mô tả này.";
            }

            ViewBag.TuKhoa = $"AI nhận diện ảnh: \"{imageDescription}\"";
            return View("Search", ketQua);
        }
    }
}