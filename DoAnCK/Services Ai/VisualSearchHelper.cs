using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DoAnCK.Services_Ai
{
    public static class VisualSearchHelper
    {
        // Dán API KEY Gemini của nhóm bạn vào đây
        // Lấy từ Google AI Studio
        private const string GeminiApiKey = "";

        public static async Task<string> DescribeImageWithGeminiAsync(byte[] imageBytes)
        {
            return await DescribeImageWithGeminiAsync(imageBytes, "image/jpeg");
        }

        public static async Task<string> DescribeImageWithGeminiAsync(byte[] imageBytes, string mimeType)
        {
            try
            {
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return "ERROR: Không nhận được dữ liệu ảnh.";
                }

                if (string.IsNullOrWhiteSpace(GeminiApiKey) ||
                    GeminiApiKey == "DAN_API_KEY_GEMINI_CUA_BAN_VAO_DAY")
                {
                    return "ERROR: Chưa cấu hình Gemini API Key trong VisualSearchHelper.cs.";
                }

                if (string.IsNullOrWhiteSpace(mimeType))
                {
                    mimeType = "image/jpeg";
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string base64Image = Convert.ToBase64String(imageBytes);

                string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent";

                JObject requestBody = new JObject
                {
                    ["contents"] = new JArray
                    {
                        new JObject
                        {
                            ["parts"] = new JArray
                            {
                                new JObject
                                {
                                    ["text"] =
                                        "Bạn là AI hỗ trợ tìm kiếm sản phẩm nội thất. " +
                                        "Hãy mô tả ngắn gọn ảnh này bằng tiếng Việt, tập trung vào loại sản phẩm, màu sắc, chất liệu, kiểu dáng và không gian sử dụng. " +
                                        "Chỉ trả về một câu mô tả, không giải thích dài."
                                },
                                new JObject
                                {
                                    ["inline_data"] = new JObject
                                    {
                                        ["mime_type"] = mimeType,
                                        ["data"] = base64Image
                                    }
                                }
                            }
                        }
                    }
                };

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "POST";
                request.ContentType = "application/json";

                // Quan trọng: Gemini dùng x-goog-api-key, không dùng Authorization Bearer
                request.Headers.Add("x-goog-api-key", GeminiApiKey);

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

                        if (string.IsNullOrWhiteSpace(result))
                        {
                            return "ERROR: Gemini không trả về mô tả ảnh.";
                        }

                        return result.Trim();
                    }
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (HttpWebResponse errorResponse = (HttpWebResponse)wex.Response)
                    {
                        using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string errorText = await reader.ReadToEndAsync();

                            if (errorText.Contains("API_KEY_INVALID") ||
                                errorText.Contains("UNAUTHENTICATED") ||
                                errorText.Contains("invalid authentication"))
                            {
                                return "ERROR: Gemini API Key không hợp lệ hoặc chưa có quyền truy cập. Hãy kiểm tra lại API key trong VisualSearchHelper.cs.";
                            }

                            return "ERROR: Gemini API lỗi: " + errorText;
                        }
                    }
                }

                return "ERROR: Không kết nối được Gemini API: " + wex.Message;
            }
            catch (Exception ex)
            {
                return "ERROR: Lỗi xử lý tìm kiếm hình ảnh: " + ex.Message;
            }
        }
    }
}