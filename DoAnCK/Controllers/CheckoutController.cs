using System.Web.Mvc;
using TMDT_NoiThatB2C.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using TMDT_NoiThatB2C.Others;
using System;

namespace TMDT_NoiThatB2C.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly string clientId = "963ad281-b310-401e-b0ee-31eb468b5af4";
        private readonly string apiKey = "afa61979-5734-490b-a74f-2dbc63bcee9a";
        private readonly string checksumKey = "c543059cd37babf573bd1bf455e6b75ccf915b0174553df68d9deac64ee7d20d";
        [HttpGet]
        public ActionResult Index()
        {
            // LẤY GIỎ HÀNG THỰC TẾ TỪ SESSION
            var cartItems = Session["ShoppingCartSession"] as List<CartItemViewModel> ?? new List<CartItemViewModel>();

            // Nếu giỏ hàng trống, bắt quay lại trang chủ hoặc trang sản phẩm
            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new CheckoutViewModel
            {
                ShippingFee = 50000,   // Có thể để mặc định hoặc tính theo khu vực
                BulkyFeePerItem = 200000,
                CartItems = cartItems  // Truyền nguyên cái giỏ hàng vào đây
            };

            return View(model);
        }

        // 2. Hàm nhận dữ liệu từ AJAX gửi lên khi bấm "Đặt Hàng"
        [HttpPost]
        public ActionResult PlaceOrder(string FullName, string PhoneNumber, string Address, string PaymentMethod)
        {
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(PhoneNumber))
            {
                return Json(new { success = false, message = "Vui lòng điền đủ thông tin!" });
            }

            // TODO: Xử lý lưu đơn hàng, lưu chi tiết đơn hàng vào Database tại đây...

            // Trả về kết quả cho JavaScript
            return Json(new { success = true, message = "Đặt hàng thành công!" });
        }
        [HttpPost]
        public ActionResult CalculatePriceByLocation(string cityName)
        {
            // Lấy giỏ hàng từ Session
            var cartItems = Session["ShoppingCartSession"] as List<CartItemViewModel> ?? new List<CartItemViewModel>();

            decimal subTotal = cartItems.Sum(item => item.Price * item.Quantity);
            bool hasBulkyItem = cartItems.Any(item => item.IsBulky);
            decimal bulkyFee = hasBulkyItem ? 200000 : 0;

            // CÔNG THỨC TÍNH PHÍ SHIP THEO TỈNH/THÀNH PHỐ
            decimal shippingFee = 0;

            // Chuẩn hóa tên để dễ so sánh (chống lỗi do API trả về có chữ "Thành phố")
            string location = cityName.ToLower();

            if (location.Contains("hồ chí minh"))
            {
                // Nội thành TP.HCM (Ví dụ: 30k)
                shippingFee = 30000;
            }
            else if (location.Contains("bình dương") || location.Contains("đồng nai") || location.Contains("long an"))
            {
                // Các tỉnh lân cận TP.HCM (Ví dụ: 50k)
                shippingFee = 50000;
            }
            else
            {
                // Các tỉnh thành khác trên toàn quốc (Ví dụ: 100k)
                shippingFee = 100000;
            }

            // Tính tổng tiền cuối cùng
            decimal totalPrice = subTotal + shippingFee + bulkyFee;

            return Json(new
            {
                success = true,
                shippingFee = shippingFee,
                totalPrice = totalPrice
            });
        }


        [HttpPost]
        public ActionResult PaymentWithMoMo(string FullName, string PhoneNumber, string Address, decimal TotalPrice)
        {
            try
            {
                // 1. DÒNG NÀY CỰC KỲ QUAN TRỌNG: Ép .NET dùng bảo mật TLS 1.2 để MoMo không từ chối kết nối
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                string endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
                string partnerCode = "MOMO5RGX20191128";
                string accessKey = "M8brj9K6E22vXoDB";
                string secretKey = "nqQiVSgDMy809JoPF6OzP5OdPdBPuwE";
                string orderInfo = "Khách hàng: " + FullName + " thanh toán nội thất MODERNO";

                string orderId = DateTime.Now.Ticks.ToString();
                //string amount = TotalPrice.ToString("0");

                decimal momoAmount = TotalPrice;

                // Nếu tổng tiền lớn hơn 50 triệu, ép nó về đúng 50 triệu để lách luật của MoMo Sandbox
                if (momoAmount > 50000000)
                {
                    momoAmount = 50000000;
                }

                string amount = momoAmount.ToString("0");

                string requestId = DateTime.Now.Ticks.ToString();

                // SỬA PORT 61802 THÀNH ĐÚNG PORT MÁY BẠN NHÉ
                string redirectUrl = "https://localhost:61802/Checkout/MoMoReturn";
                string ipnUrl = "https://localhost:61802/Checkout/MoMoReturn";
                string requestType = "captureWallet";
                string extraData = "";

                string rawHash = "accessKey=" + accessKey +
                    "&amount=" + amount +
                    "&extraData=" + extraData +
                    "&ipnUrl=" + ipnUrl +
                    "&orderId=" + orderId +
                    "&orderInfo=" + orderInfo +
                    "&partnerCode=" + partnerCode +
                    "&redirectUrl=" + redirectUrl +
                    "&requestId=" + requestId +
                    "&requestType=" + requestType;

                string signature = TMDT_NoiThatB2C.Others.MoMoSecurity.signSHA256(rawHash, secretKey);

                Newtonsoft.Json.Linq.JObject message = new Newtonsoft.Json.Linq.JObject
        {
            { "partnerCode", partnerCode },
            { "partnerName", "MODERNO" },
            { "storeId", "MomoTestStore" },
            { "requestId", requestId },
            { "amount", amount },
            { "orderId", orderId },
            { "orderInfo", orderInfo },
            { "redirectUrl", redirectUrl },
            { "ipnUrl", ipnUrl },
            { "lang", "vi" },
            { "extraData", extraData },
            { "requestType", requestType },
            { "signature", signature }
        };

                string responseFromMomo = SendPaymentRequest(endpoint, message.ToString());
                Newtonsoft.Json.Linq.JObject jmessage = Newtonsoft.Json.Linq.JObject.Parse(responseFromMomo);

                string payUrl = jmessage.GetValue("payUrl").ToString();

                return Json(new { success = true, url = payUrl });
            }
            catch (System.Net.WebException wex)
            {
                // Bắt lỗi chuyên sâu từ API trả về
                if (wex.Response != null)
                {
                    using (var errorResponse = (System.Net.HttpWebResponse)wex.Response)
                    {
                        using (var reader = new System.IO.StreamReader(errorResponse.GetResponseStream()))
                        {
                            string errorText = reader.ReadToEnd();
                            // errorText này sẽ chứa câu trả lời chi tiết của MoMo (ví dụ: "amount vượt quá giới hạn...")
                            return Json(new { success = false, message = errorText });
                        }
                    }
                }
                return Json(new { success = false, message = wex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // HÀM HỖ TRỢ: Gửi Request POST tới MoMo
        private string SendPaymentRequest(string endpoint, string postJsonString)
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(endpoint);
            var postData = postJsonString;
            var data = Encoding.UTF8.GetBytes(postData);

            httpWReq.ProtocolVersion = HttpVersion.Version11;
            httpWReq.Method = "POST";
            httpWReq.ContentType = "application/json";
            httpWReq.ContentLength = data.Length;
            httpWReq.ReadWriteTimeout = 30000;
            httpWReq.Timeout = 15000;

            Stream stream = httpWReq.GetRequestStream();
            stream.Write(data, 0, data.Length);
            stream.Close();

            HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse();
            string jsonresponse = "";
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string temp = null;
                while ((temp = reader.ReadLine()) != null)
                {
                    jsonresponse += temp;
                }
            }
            return jsonresponse;
        }

        // 6. HÀM XỬ LÝ KHI KHÁCH QUÉT MÃ TRẢ TIỀN XONG, MOMO ĐẨY VỀ LẠI WEB
        public ActionResult MoMoReturn()
        {
            string errorCode = Request.QueryString["resultCode"];
            string orderId = Request.QueryString["orderId"];

            if (errorCode == "0") // 0 là thanh toán thành công
            {
                ViewBag.Message = "Cảm ơn bạn đã thanh toán. Đơn hàng " + orderId + " đang được xử lý!";
                // TODO: Xóa session giỏ hàng, Cập nhật trạng thái đơn hàng trong Database
                Session["ShoppingCartSession"] = null;
            }
            else
            {
                ViewBag.Message = "Thanh toán thất bại hoặc đã bị hủy!";
            }

            return View(); // Nhớ tạo một cái View MoMoReturn.cshtml đơn giản để hiện lời cảm ơn nhé
        }
        // HÀM 1: TỰ ĐỘNG TẠO MÃ VIETQR ĐỘNG (MIỄN PHÍ)
        [HttpPost]
        public ActionResult CreateVietQR(string FullName, string PhoneNumber, string Address, decimal TotalPrice)
        {
            try
            {
                string orderCode = "MODERNO" + DateTime.Now.ToString("ddMMHHmm"); // Mã nội dung chuyển khoản tự động

                // Cấu hình thông tin ngân hàng của bạn
                string bankId = "VCB"; // Mã Vietcombank (Xem thêm mã các ngân hàng khác tại VietQR)
                string accountNo = "123456789"; // Số tài khoản thật hoặc giả lập của bạn
                string template = "qr_only"; // Chỉ lấy ảnh QR cho gọn giao diện

                // Link API tạo VietQR tự động theo chuẩn Napas247
                string qrUrl = $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={TotalPrice}&addInfo={orderCode}&accountName=Moderno%20Store";

                // TODO: Bạn có thể viết code lưu thông tin đơn hàng vào Database ở đây với trạng thái "Chờ thanh toán"

                return Json(new { success = true, qrUrl = qrUrl, orderCode = orderCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // HÀM 2: XỬ LÝ ĐƠN HÀNG COD (TIỀN MẶT)
        [HttpPost]
        public ActionResult ProcessCOD(string FullName, string PhoneNumber, string Address, decimal TotalPrice)
        {
            string orderCode = "MDN" + DateTime.Now.ToString("ddMMHHmm");

            // TODO: Viết code tạo bản ghi dữ liệu Đơn hàng (Order) và Chi tiết đơn hàng (OrderDetail) vào SQL Server ở đây
            // Ví dụ: 
            // var order = new Order { Code = orderCode, CustomerName = FullName, Total = TotalPrice, PaymentMethod = "COD", Status = "Pending" };
            // _db.Orders.Add(order);
            // _db.SaveChanges();

            // Xóa giỏ hàng sau khi đặt thành công
            Session["ShoppingCartSession"] = null;

            return Json(new { success = true, orderCode = orderCode });
        }

        // HÀM 3: TRANG HIỂN THỊ THÀNH CÔNG ĐỒNG BỘ
        public ActionResult OrderSuccess(string orderCode)
        {
            ViewBag.OrderCode = orderCode;
            return View();
        }
        [HttpPost]
        public ActionResult PaymentWithPayOS(string FullName, string PhoneNumber, string Address, decimal TotalPrice)
        {
            try
            {
                // Bắt buộc cấu hình giao thức bảo mật mạng để kết nối API
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // 1. Chuẩn bị dữ liệu đơn hàng theo cấu trúc PayOS đòi hỏi
                long orderCode = DateTime.Now.Ticks % 100000000; // Mã đơn hàng dạng số nguyên
                int amount = (int)TotalPrice;

                // Thu hẹp số tiền nếu bạn muốn test chuyển khoản thật 2.000 ₫ cho đỡ tốn tiền thật
                //if (amount > 50000) amount = 2000;


         

                string description = $"Moderno {orderCode}";
                string cancelUrl = "https://localhost:61802/Checkout/Index";
                string returnUrl = "https://localhost:61802/Checkout/OrderSuccess";

                // 2. Tạo chữ ký bảo mật SHA256 để bảo vệ dữ liệu (Sử dụng hàm băm chuỗi dữ liệu kèm ChecksumKey)
                string rawSignatureData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
                string signature = Others.MoMoSecurity.signSHA256(rawSignatureData, checksumKey);

                // 3. Đóng gói đối tượng JSON để gửi đi
                JObject body = new JObject
                {
                    { "orderCode", orderCode },
                    { "amount", amount },
                    { "description", description },
                    { "cancelUrl", cancelUrl },
                    { "returnUrl", returnUrl },
                    { "signature", signature }
                };

                // 4. Thực hiện lệnh POST gửi dữ liệu sang máy chủ PayOS
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api-merchant.payos.vn/v2/payment-requests");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("x-client-id", clientId);
                request.Headers.Add("x-api-key", apiKey);

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(body.ToString());
                }

                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    JObject jsonResult = JObject.Parse(result);

                    if (jsonResult["code"].ToString() == "00")
                    {
                        // Lấy đường link trang thanh toán chứa mã VietQR động
                        string checkoutUrl = jsonResult["data"]["checkoutUrl"].ToString();
                        return Json(new { success = true, url = checkoutUrl });
                    }

                    return Json(new { success = false, message = jsonResult["desc"].ToString() });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}