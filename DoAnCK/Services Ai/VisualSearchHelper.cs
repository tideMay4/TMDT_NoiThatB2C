using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DoAnCK.Services_Ai
{
    public static class VisualSearchHelper
    {
        // Sử dụng API Key mới đang hoạt động trơn tru
        private const string GeminiApiKey = "";

        public static async Task<string> DescribeImageWithGeminiAsync(byte[] imageBytes)
        {
            return await DescribeImageWithGeminiAsync(imageBytes, "image/jpeg");
        }

        public static async Task<string> DescribeImageWithGeminiAsync(byte[] imageBytes, string mimeType)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return "ERROR: Không nhận được dữ liệu ảnh.";
            }

            if (string.IsNullOrWhiteSpace(mimeType))
            {
                mimeType = "image/jpeg";
            }

            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                string base64Image = Convert.ToBase64String(imageBytes);

                // CÂU LỆNH ĐÃ ĐƯỢC CHỈNH SỬA: Ép AI trả về đúng 1 cụm từ, cấm miêu tả phòng khách hay đồ vật phụ
                var requestObject = new
                {
                    contents = new[] {
                        new {
                            parts = new object[] {
                                new { text = "Bạn là chuyên gia nhận diện sản phẩm nội thất. Hãy nhìn vào món đồ vật LỚN NHẤT, CHÍNH YẾU NHẤT trong ảnh và trả về ĐÚNG 1 CỤM TỪ KHÓA, không được viết thành câu. Cú pháp bắt buộc: [Loại sản phẩm] + [Màu sắc hoặc Chất liệu]. Ví dụ: 'Sofa màu xanh lam', 'Bàn trà gỗ'. TUYỆT ĐỐI KHÔNG miêu tả cảnh vật, KHÔNG nhắc đến không gian phòng khách, KHÔNG nhắc đến các vật dụng đi kèm." },
                                new { inlineData = new { mimeType = mimeType, data = base64Image } }
                            }
                        }
                    }
                };

                string jsonPayload = JsonConvert.SerializeObject(requestObject);

                // Áp dụng cơ chế vòng lặp fallback thông minh
                string[] candidateModels = { "gemini-2.0-flash", "gemini-flash-latest", "gemini-1.5-flash" };
                string errorLogs = "";

                using (var client = new HttpClient())
                {
                    foreach (var model in candidateModels)
                    {
                        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={GeminiApiKey}";
                        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync(url, content);
                        string resStr = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            JObject json = JObject.Parse(resStr);
                            string result = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                            if (!string.IsNullOrWhiteSpace(result))
                            {
                                return result.Trim();
                            }
                        }
                        else
                        {
                            errorLogs += $"[{model} lỗi {response.StatusCode}: {resStr}] ";
                        }
                    }
                }

                return $"ERROR: {errorLogs}";
            }
            catch (Exception ex)
            {
                return "ERROR: Lỗi xử lý tìm kiếm hình ảnh: " + ex.Message;
            }
        }
    }
}