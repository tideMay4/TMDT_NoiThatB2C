using System;
using System.Collections.Generic;
using System.Configuration;
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

        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        private readonly string clientId = ConfigurationManager.AppSettings["PayOSClientId"] ?? "";
        private readonly string apiKey = ConfigurationManager.AppSettings["PayOSApiKey"] ?? "";
        private readonly string checksumKey = ConfigurationManager.AppSettings["PayOSChecksumKey"] ?? "";

        [HttpGet]
        public ActionResult Index()
        {
            var cartItems = Session[CartSessionKey] as List<CartItemViewModel>;

            if (cartItems == null || cartItems.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            decimal subTotal = cartItems.Sum(x => x.Price * x.Quantity);
            decimal shippingFee = 100000;
            decimal bulkyFee = CalculateBulkyFee(cartItems);
            decimal assemblyFee = 0;
            decimal totalFee = shippingFee + bulkyFee + assemblyFee;
            decimal grandTotal = subTotal + totalFee;

            CheckoutViewModel model = new CheckoutViewModel
            {
                CartItems = cartItems,
                SubTotal = subTotal,
                ShippingFee = shippingFee,
                BulkyFee = bulkyFee,
                AssemblyFee = assemblyFee,
                TotalFee = totalFee,
                GrandTotal = grandTotal,
                YeuCauLapRap = false
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
        public ActionResult CalculateCheckoutFee(string address, bool yeuCauLapRap)
        {
            var cartItems = Session[CartSessionKey] as List<CartItemViewModel>;

            if (cartItems == null || cartItems.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Giỏ hàng đang trống."
                });
            }

            decimal subTotal = cartItems.Sum(x => x.Price * x.Quantity);
            decimal shippingFee = CalculateShippingFee(address);
            decimal bulkyFee = CalculateBulkyFee(cartItems);
            decimal assemblyFee = CalculateAssemblyFee(cartItems, yeuCauLapRap);
            decimal totalFee = shippingFee + bulkyFee + assemblyFee;
            decimal grandTotal = subTotal + totalFee;

            return Json(new
            {
                success = true,
                subTotal = subTotal,
                shippingFee = shippingFee,
                bulkyFee = bulkyFee,
                assemblyFee = assemblyFee,
                totalFee = totalFee,
                grandTotal = grandTotal
            });
        }

        [HttpPost]
        public ActionResult ProcessCOD(string FullName, string PhoneNumber, string Address, decimal TotalPrice, bool YeuCauLapRap = false)
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

            var cartItems = Session[CartSessionKey] as List<CartItemViewModel>;

            if (cartItems == null || cartItems.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Giỏ hàng của bạn đang trống."
                });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int? maKH = null;

                    if (Session["MaKH"] != null)
                    {
                        maKH = Convert.ToInt32(Session["MaKH"]);
                    }

                    if (maKH == null && Session["MaTK"] != null)
                    {
                        int maTK = Convert.ToInt32(Session["MaTK"]);

                        var khachHang = db.KHACHHANGs.FirstOrDefault(x => x.MaTK == maTK);

                        if (khachHang != null)
                        {
                            maKH = khachHang.MaKH;
                        }
                    }

                    if (maKH == null)
                    {
                        string phone = PhoneNumber.Trim();
                        string guestEmail = phone + "@guest.local";

                        var taiKhoanCu = db.TAIKHOANs.FirstOrDefault(x =>
                            x.Email == guestEmail || x.SDT == phone
                        );

                        if (taiKhoanCu != null)
                        {
                            var khachHangCu = db.KHACHHANGs.FirstOrDefault(x => x.MaTK == taiKhoanCu.MaTK);

                            if (khachHangCu != null)
                            {
                                khachHangCu.HoTen = FullName.Trim();
                                khachHangCu.SDT = phone;
                                khachHangCu.DiaChi = Address.Trim();

                                maKH = khachHangCu.MaKH;
                            }
                            else
                            {
                                KHACHHANG khMoi = new KHACHHANG
                                {
                                    MaTK = taiKhoanCu.MaTK,
                                    HoTen = FullName.Trim(),
                                    SDT = phone,
                                    DiaChi = Address.Trim(),
                                    NgayDangKy = DateTime.Now
                                };

                                db.KHACHHANGs.Add(khMoi);
                                db.SaveChanges();

                                maKH = khMoi.MaKH;
                            }
                        }
                        else
                        {
                            TAIKHOAN tkMoi = new TAIKHOAN
                            {
                                HoTen = FullName.Trim(),
                                Email = guestEmail,
                                MatKhau = "123456",
                                SDT = phone,
                                VaiTro = "Customer",
                                TrangThai = true,
                                NgayTao = DateTime.Now
                            };

                            db.TAIKHOANs.Add(tkMoi);
                            db.SaveChanges();

                            KHACHHANG khMoi = new KHACHHANG
                            {
                                MaTK = tkMoi.MaTK,
                                HoTen = FullName.Trim(),
                                SDT = phone,
                                DiaChi = Address.Trim(),
                                NgayDangKy = DateTime.Now
                            };

                            db.KHACHHANGs.Add(khMoi);
                            db.SaveChanges();

                            maKH = khMoi.MaKH;
                        }
                    }

                    decimal subTotal = cartItems.Sum(x => x.Price * x.Quantity);
                    decimal shippingFee = CalculateShippingFee(Address);
                    decimal bulkyFee = CalculateBulkyFee(cartItems);
                    decimal assemblyFee = CalculateAssemblyFee(cartItems, YeuCauLapRap);
                    decimal totalFee = shippingFee + bulkyFee + assemblyFee;
                    decimal grandTotal = subTotal + totalFee;

                    DONHANG donHang = new DONHANG
                    {
                        MaKH = maKH.Value,
                        NgayDat = DateTime.Now,
                        DiaChiGiaoHang = Address.Trim(),
                        GhiChu = null,
                        TongTien = grandTotal,
                        TrangThai = "Chờ xác nhận"
                    };

                    db.DONHANGs.Add(donHang);
                    db.SaveChanges();

                    VANCHUYEN vanChuyen = new VANCHUYEN
                    {
                        MaDH = donHang.MaDH,
                        TenDonViVanChuyen = "MODERNO Delivery",
                        MaVanDon = "VC" + donHang.MaDH.ToString("000000"),
                        PhiVanChuyen = shippingFee,
                        PhiCongKenh = bulkyFee,
                        PhiLapRap = assemblyFee,
                        TongPhiVanChuyen = totalFee,
                        NgayGiaoDuKien = DateTime.Now.AddDays(3),
                        NgayGiaoThucTe = null,
                        TrangThai = "Chờ xử lý"
                    };

                    db.VANCHUYENs.Add(vanChuyen);
                    db.SaveChanges();

                    foreach (var item in cartItems)
                    {
                        int? storeId = item.MaTK_Store;

                        if (!storeId.HasValue)
                        {
                            storeId = db.Database.SqlQuery<int?>(
                                @"SELECT TOP 1 MaTK_Store
                                  FROM CUAHANG_SANPHAM
                                  WHERE MaSP = @p0
                                    AND TrangThai = 1
                                  ORDER BY GiaBan ASC",
                                item.ProductId
                            ).FirstOrDefault();
                        }

                        db.Database.ExecuteSqlCommand(
                            @"INSERT INTO CHITIET_DONHANG
                              (
                                  MaDH,
                                  MaSP,
                                  MaTK_Store,
                                  SoLuong,
                                  GiaBan,
                                  ThanhTien
                              )
                              VALUES
                              (
                                  @p0,
                                  @p1,
                                  @p2,
                                  @p3,
                                  @p4,
                                  @p5
                              )",
                            donHang.MaDH,
                            item.ProductId,
                            storeId,
                            item.Quantity,
                            item.Price,
                            item.Price * item.Quantity
                        );

                        if (storeId.HasValue)
                        {
                            db.Database.ExecuteSqlCommand(
                                @"UPDATE CUAHANG_SANPHAM
                                  SET SoLuongTon = CASE
                                                      WHEN SoLuongTon - @p0 < 0 THEN 0
                                                      ELSE SoLuongTon - @p0
                                                    END,
                                      NgayCapNhat = GETDATE()
                                  WHERE MaSP = @p1
                                    AND MaTK_Store = @p2",
                                item.Quantity,
                                item.ProductId,
                                storeId.Value
                            );
                        }

                        var sanPham = db.SANPHAMs.Find(item.ProductId);

                        if (sanPham != null)
                        {
                            sanPham.SoLuongTon = sanPham.SoLuongTon - item.Quantity;

                            if (sanPham.SoLuongTon < 0)
                            {
                                sanPham.SoLuongTon = 0;
                            }
                        }
                    }

                    db.SaveChanges();

                    transaction.Commit();

                    Session[CartSessionKey] = null;

                    string orderCode = "ORD-" + donHang.MaDH;

                    return Json(new
                    {
                        success = true,
                        orderCode = orderCode
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    return Json(new
                    {
                        success = false,
                        message = "Lỗi khi lưu đơn hàng: " + ex.Message
                    });
                }
            }
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
                if (string.IsNullOrWhiteSpace(clientId) ||
                    string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(checksumKey))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Chưa cấu hình PayOS trong Web.config."
                    });
                }

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

        private decimal CalculateShippingFee(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return 100000;
            }

            string lowerAddress = address.ToLower();

            if (lowerAddress.Contains("tp.hcm") ||
                lowerAddress.Contains("hồ chí minh") ||
                lowerAddress.Contains("ho chi minh"))
            {
                return 30000;
            }

            if (lowerAddress.Contains("bình dương") ||
                lowerAddress.Contains("đồng nai") ||
                lowerAddress.Contains("long an"))
            {
                return 50000;
            }

            return 100000;
        }

        private decimal CalculateBulkyFee(List<CartItemViewModel> cartItems)
        {
            return cartItems
                .Where(x => x.IsBulky)
                .Sum(x => x.PhiCongKenh * x.Quantity);
        }

        private decimal CalculateAssemblyFee(List<CartItemViewModel> cartItems, bool yeuCauLapRap)
        {
            if (!yeuCauLapRap)
            {
                return 0;
            }

            return cartItems
                .Where(x => x.CanLapRap)
                .Sum(x => x.PhiLapRap * x.Quantity);
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