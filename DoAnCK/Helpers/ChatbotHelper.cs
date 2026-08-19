using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DoAnCK.Services_Ai
{
    public static class ChatbotHelper
    {
        // Key này của bạn Google đã xác nhận hợp lệ
        private const string GeminiApiKey = "";

        // Phiên bản gemini-1.5-flash là model chat tốt nhất và nhanh nhất hiện tại
        // Lựa chọn 1 (Ổn định nhất): Dùng gemini-pro
        // Lựa chọn 1 (Ổn định nhất): Dùng gemini-pro
        // Sử dụng model Gemini 3.6 Flash mới nhất và mạnh mẽ nhất
        private const string GeminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent";
        private static readonly string SystemPrompt = @"
Bạn là trợ lý AI ảo của website bán đồ nội thất Moderno.
Nhiệm vụ của bạn là tư vấn khách hàng một cách chuyên nghiệp, lịch sự và chính xác.

BẠN CHỈ ĐƯỢC PHÉP TRẢ LỜI CÁC CHỦ ĐỀ SAU:
- Sản phẩm, Danh mục, Giá, Khuyến mãi.
- Đặt hàng, Thanh toán, Vận chuyển, Bảo hành, Chính sách đổi trả.

CÁC QUY TẮC NGHIÊM NGẶT:
1. TỪ CHỐI NGOÀI PHẠM VI: Nếu người dùng hỏi ngoài phạm vi trên, hãy lịch sự từ chối và hướng họ quay lại nội dung liên quan đến website.
2. KHÔNG BỊA ĐẶT DỮ LIỆU: Không tự bịa thông tin, không tự tạo giá.
3. DỮ LIỆU CUNG CẤP: Chỉ sử dụng thông tin sản phẩm trong [Dữ liệu cửa hàng].
4. ĐỊNH DẠNG: Trả lời bằng tiếng Việt, ngắn gọn, dễ hiểu.
";

        public static async Task<string> GetChatbotResponseAsync(string userMessage, string contextData)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string combinedPrompt = $"{SystemPrompt}\n\n[Dữ liệu cửa hàng hiện tại]:\n{contextData}\n\n[Khách hàng hỏi]: {userMessage}";

                JObject requestBody = new JObject
                {
                    ["contents"] = new JArray
                    {
                        new JObject
                        {
                            ["parts"] = new JArray
                            {
                                new JObject { ["text"] = combinedPrompt }
                            }
                        }
                    }
                };

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"{GeminiEndpoint}?key={GeminiApiKey}");
                request.Method = "POST";
                request.ContentType = "application/json";

                byte[] requestBytes = Encoding.UTF8.GetBytes(requestBody.ToString());
                request.ContentLength = requestBytes.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(requestBytes, 0, requestBytes.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string responseText = await reader.ReadToEndAsync();
                        JObject json = JObject.Parse(responseText);
                        string result = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                        return string.IsNullOrWhiteSpace(result) ? "Xin lỗi, hiện tại tôi không thể trả lời." : result.Trim();
                    }
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        string errorText = reader.ReadToEnd();
                        return "Lỗi từ Google: " + errorText;
                    }
                }
                return "Lỗi mạng: " + wex.Message;
            }
            catch (Exception ex)
            {
                return "Lỗi C#: " + ex.Message;
            }
        }
    }
}