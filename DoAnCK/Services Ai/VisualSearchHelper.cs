using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DoAnCK.Services_Ai
{
    public class VisualSearchHelper
    {
        private static readonly string GeminiApiKey = "AQ.Ab8RN6KyAHFH6Il-2-Ki2oaN55y67CLrOWxev3HBP7yevU9kDg";

        public static async Task<string> DescribeImageWithGeminiAsync(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return "ERROR: Dữ liệu ảnh rỗng.";

            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                string base64Image = Convert.ToBase64String(imageBytes);

                var requestObject = new
                {
                    contents = new[] {
                        new {
                            parts = new object[] {
                                new { text = "Mô tả ngắn gọn đồ nội thất trong ảnh bằng tiếng Việt (loại đồ, màu sắc). Trả về 1 câu." },
                                new { inlineData = new { mimeType = "image/jpeg", data = base64Image } }
                            }
                        }
                    }
                };

                string jsonPayload = JsonConvert.SerializeObject(requestObject);

                string[] candidateModels = { "gemini-2.0-flash", "gemini-flash-latest" };
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
                            string text = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
                        }
                        else
                        {
                            // Đã cập nhật dòng này để Google báo cáo chính xác lý do
                            errorLogs += $"[{model} lỗi {response.StatusCode}: {resStr}] ";
                        }
                    }
                }
                return $"ERROR: {errorLogs}";
            }
            catch (Exception ex)
            {
                return $"ERROR: Lỗi C# - {ex.Message}";
            }
        }
    }
}