using DoAnCK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DoAnCK.Services_Ai
{
    public class AIEmbeddingHelper
    {
        private static readonly string GeminiApiKey = "AQ.Ab8RN6KyAHFH6Il-2-Ki2oaN55y67CLrOWxev3HBP7yevU9kDg";
        // Sử dụng mô hình gemini-embedding-2 mới nhất của Google
        private static readonly string GeminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-2:embedContent?key={GeminiApiKey}";
        // Bộ nhớ đệm tĩnh lưu trữ cấu trúc: MaSP -> Bộ Vector tương ứng
        public static Dictionary<int, float[]> ProductVectorCache = new Dictionary<int, float[]>();

        /// <summary>
        /// Hàm tự động quét toàn bộ sản phẩm đang hoạt động trong SQL Server, 
        /// gửi lên Gemini lấy Vector và nạp vào RAM (Chỉ chạy nạp lần đầu tiên).
        /// </summary>
        public static async Task InitCacheAsync(DoAnNoiThatB2CEntities db)
        {
            if (ProductVectorCache.Count > 0) return; // Nếu đã nạp rồi thì bỏ qua

            var dsSanPham = db.SANPHAMs.Where(x => x.TrangThai == true).ToList();
            foreach (var sp in dsSanPham)
            {
                string text = GenerateProductTextForAI(sp);
                var vector = await GetVectorFromTextAsync(text);
                if (vector != null)
                {
                    ProductVectorCache[sp.MaSP] = vector;
                }
            }
        }

        public static string GenerateProductTextForAI(SANPHAM sp)
        {
            if (sp == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            string tenDanhMuc = sp.DANHMUC != null ? sp.DANHMUC.TenDM : "Chưa phân loại";

            sb.Append($"Danh mục: {tenDanhMuc}. ");
            sb.Append($"Tên sản phẩm nội thất: {sp.TenSP}. ");

            if (!string.IsNullOrEmpty(sp.ThuongHieu))
                sb.Append($"Thương hiệu: {sp.ThuongHieu}. ");

            if (!string.IsNullOrEmpty(sp.MoTa))
                sb.Append($"Chi tiết: {sp.MoTa} ");

            return sb.ToString();
        }

        public static async Task<float[]> GetVectorFromTextAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    content = new { parts = new[] { new { text = text } } }
                };

                string jsonPayload = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(GeminiUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                        var values = result.embedding.values;
                        return values.ToObject<float[]>();
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Thuật toán hình học tính góc Cosine giữa 2 mảng Vector.
        /// Kết quả trả về từ -1 đến 1. Càng gần 1 nghĩa là ý nghĩa của 2 câu càng giống nhau.
        /// </summary>
        public static double CosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length)
                return 0;

            double dotProduct = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }

            if (normA == 0.0 || normB == 0.0) return 0;

            return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}