using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using DoAnCK.Models;
using DoAnCK.Others;
using Newtonsoft.Json.Linq;


namespace DoAnCK.Controllers
{
    public class CheckoutController : Controller
    {
        private const string CartSessionKey = "ShoppingCartSession";

        // PayOS config
        private readonly string clientId = "";
        private readonly string apiKey = "";
        private readonly string checksumKey = "";

        [HttpGet]
        public ActionResult Index()
        {
            var cartItems = Session[CartSessionKey] as List<CartItemViewModel>;

            if (cartItems == null || cartItems.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                ShippingFee = 50000,
                BulkyFeePerItem = 200000
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult CalculatePriceByLocation(string cityName)
        {
            var cartItems = Session[CartSessionKey] as List<CartItemViewModel>;

            if (cartItems == null)
            {
                cartItems = new List<CartItemViewModel>();
            }

            decimal subTotal = cartItems.Sum(item => item.Price * item.Quantity);
            bool hasBulkyItem = cartItems.Any(item => item.IsBulky);
            decimal bulkyFee = hasBulkyItem ? 200000 : 0;

            decimal shippingFee = 100000;

            if (!string.IsNullOrWhiteSpace(cityName))
            {
                string location = cityName.ToLower();

                if (location.Contains("hồ chí minh"))
                {
                    shippingFee = 30000;
                }
                else if (location.Contains("bình dương") ||
                         location.Contains("đồng nai") ||
                         location.Contains("long an"))
                {
                    shippingFee = 50000;
                }
            }

            decimal totalPrice = subTotal + shippingFee + bulkyFee;

            return Json(new
            {
                success = true,
                shippingFee = shippingFee,
                bulkyFee = bulkyFee,
                totalPrice = totalPrice
            });
        }

        [HttpPost]
        public ActionResult ProcessCOD(string FullName, string PhoneNumber, string Address, decimal TotalPrice)
        {
            if (string.IsNullOrWhiteSpace(FullName) ||
                string.IsNullOrWhiteSpace(PhoneNumber) ||
                string.IsNullOrWhiteSpace(Address))
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập đầy đủ thông tin giao hàng."
                });
            }

            string orderCode = "MDN" + DateTime.Now.ToString("ddMMyyyyHHmmss");

            // TODO: Sau này lưu DONHANG và CHITIET_DONHANG vào database tại đây.

            Session[CartSessionKey] = null;

            return Json(new
            {
                success = true,
                orderCode = orderCode
            });
        }

        public ActionResult OrderSuccess(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                orderCode = Request.QueryString["orderCode"];
            }

            ViewBag.OrderCode = orderCode;

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                Session[CartSessionKey] = null;
            }

            return View();
        }

        [HttpPost]
        public ActionResult PaymentWithPayOS(string FullName, string PhoneNumber, string Address, decimal TotalPrice)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FullName) ||
                    string.IsNullOrWhiteSpace(PhoneNumber) ||
                    string.IsNullOrWhiteSpace(Address))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng nhập đầy đủ thông tin giao hàng."
                    });
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                long orderCode = DateTime.Now.Ticks % 100000000;
                int amount = (int)TotalPrice;

                if (amount <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Số tiền thanh toán không hợp lệ."
                    });
                }

                string description = "Moderno " + orderCode;

                string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

                string cancelUrl = baseUrl + Url.Action("Index", "Checkout");
                string returnUrl = baseUrl + Url.Action("OrderSuccess", "Checkout");

                string rawSignatureData =
                    "amount=" + amount +
                    "&cancelUrl=" + cancelUrl +
                    "&description=" + description +
                    "&orderCode=" + orderCode +
                    "&returnUrl=" + returnUrl;

                string signature = MoMoSecurity.signSHA256(rawSignatureData, checksumKey);

                JObject body = new JObject
        {
            { "orderCode", orderCode },
            { "amount", amount },
            { "description", description },
            { "cancelUrl", cancelUrl },
            { "returnUrl", returnUrl },
            { "signature", signature }
        };

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api-merchant.payos.vn/v2/payment-requests");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add("x-client-id", clientId);
                request.Headers.Add("x-api-key", apiKey);

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(body.ToString());
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        JObject jsonResult = JObject.Parse(result);

                        if (jsonResult["code"] != null && jsonResult["code"].ToString() == "00")
                        {
                            string checkoutUrl = jsonResult["data"]["checkoutUrl"].ToString();

                            return Json(new
                            {
                                success = true,
                                url = checkoutUrl
                            });
                        }

                        string message = jsonResult["desc"] != null
                            ? jsonResult["desc"].ToString()
                            : "Không thể tạo thanh toán PayOS.";

                        return Json(new
                        {
                            success = false,
                            message = message
                        });
                    }
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string errorText = reader.ReadToEnd();

                            return Json(new
                            {
                                success = false,
                                message = errorText
                            });
                        }
                    }
                }

                return Json(new
                {
                    success = false,
                    message = wex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public ActionResult CreateVietQR(string FullName, string PhoneNumber, string Address, decimal TotalPrice)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FullName) ||
                    string.IsNullOrWhiteSpace(PhoneNumber) ||
                    string.IsNullOrWhiteSpace(Address))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng nhập đầy đủ thông tin giao hàng."
                    });
                }

                string orderCode = "MODERNO" + DateTime.Now.ToString("ddMMHHmmss");

                string bankId = "VCB";
                string accountNo = "123456789";
                string accountName = "Moderno Store";
                string template = "compact";

                string amountText = TotalPrice.ToString("0", CultureInfo.InvariantCulture);
                string addInfo = HttpUtility.UrlEncode(orderCode);
                string encodedAccountName = HttpUtility.UrlEncode(accountName);

                string qrUrl =
                    "https://img.vietqr.io/image/" +
                    bankId + "-" +
                    accountNo + "-" +
                    template +
                    ".png?amount=" + amountText +
                    "&addInfo=" + addInfo +
                    "&accountName=" + encodedAccountName;

                return Json(new
                {
                    success = true,
                    qrUrl = qrUrl,
                    orderCode = orderCode,
                    bankName = "Vietcombank",
                    accountNo = accountNo,
                    accountName = accountName,
                    amount = TotalPrice
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

       

        private string SendPaymentRequest(string endpoint, string postJsonString)
        {
            HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(endpoint);

            var data = Encoding.UTF8.GetBytes(postJsonString);

            httpWReq.ProtocolVersion = HttpVersion.Version11;
            httpWReq.Method = "POST";
            httpWReq.ContentType = "application/json";
            httpWReq.ContentLength = data.Length;
            httpWReq.ReadWriteTimeout = 30000;
            httpWReq.Timeout = 15000;

            using (Stream stream = httpWReq.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)httpWReq.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public ActionResult MoMoReturn()
        {
            string resultCode = Request.QueryString["resultCode"];
            string orderId = Request.QueryString["orderId"];

            if (resultCode == "0")
            {
                Session[CartSessionKey] = null;
                return RedirectToAction("OrderSuccess", new { orderCode = orderId });
            }

            TempData["Error"] = "Thanh toán MoMo thất bại hoặc đã bị hủy.";
            return RedirectToAction("Index", "Checkout");
        }
    }
}