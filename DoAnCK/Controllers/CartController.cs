using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAnCK.Helpers;
using DoAnCK.Models;

namespace DoAnCK.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "ShoppingCartSession";

        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        private List<CartItemViewModel> GetCartFromSession()
        {
            var cart = Session[CartSessionKey] as List<CartItemViewModel>;

            if (cart == null)
            {
                cart = new List<CartItemViewModel>();
                Session[CartSessionKey] = cart;
            }

            return cart;
        }

        public ActionResult Index()
        {
            var cart = GetCartFromSession();
            ViewBag.ComboSuggestions = ComboSuggestionHelper.GetInventoryComboSuggestions(db);

            return View(cart);
        }

        private decimal ToDecimalSafe(object value)
        {
            if (value == null)
            {
                return 0;
            }

            return Convert.ToDecimal(value);
        }

        private int AddProductToCart(int productId, int quantity, int? storeId = null)
        {
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var product = db.SANPHAMs.Find(productId);

            if (product == null)
            {
                return GetCartFromSession().Sum(x => x.Quantity);
            }

            if (storeId == null)
            {
                storeId = db.Database.SqlQuery<int?>(
                    @"SELECT TOP 1 MaTK_Store
                      FROM CUAHANG_SANPHAM
                      WHERE MaSP = @p0 
                        AND TrangThai = 1 
                        AND SoLuongTon > 0
                      ORDER BY GiaBan ASC",
                    productId
                ).FirstOrDefault();
            }

            var storeProduct = db.Database.SqlQuery<StoreProductCartTemp>(
                @"SELECT TOP 1 
                      CSP.MaTK_Store,
                      CH.TenCH AS TenCuaHang,
                      CSP.GiaBan,
                      CSP.SoLuongTon
                  FROM CUAHANG_SANPHAM CSP
                  INNER JOIN CUAHANG CH ON CSP.MaTK_Store = CH.MaTK
                  WHERE CSP.MaSP = @p0
                    AND CSP.MaTK_Store = @p1
                    AND CSP.TrangThai = 1
                    AND CSP.SoLuongTon > 0",
                productId,
                storeId
            ).FirstOrDefault();

            if (storeProduct == null)
            {
                return GetCartFromSession().Sum(x => x.Quantity);
            }

            var cart = GetCartFromSession();

            var existingItem = cart.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.MaTK_Store == storeProduct.MaTK_Store &&
                string.IsNullOrEmpty(x.ComboCode)
            );

            decimal phiCongKenh = ToDecimalSafe(product.PhiCongKenhMacDinh);
            decimal phiLapRap = ToDecimalSafe(product.PhiLapRapMacDinh);

            bool laCongKenh = product.LaCongKenh == true;
            bool hoTroLapRap = product.HoTroLapRap == true;

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;

                existingItem.Price = storeProduct.GiaBan;
                existingItem.ProductName = product.TenSP;
                existingItem.HinhAnh = product.HinhAnh;
                existingItem.IsBulky = laCongKenh;
                existingItem.PhiCongKenh = phiCongKenh;
                existingItem.CanLapRap = hoTroLapRap;
                existingItem.PhiLapRap = phiLapRap;
                existingItem.MaTK_Store = storeProduct.MaTK_Store;
                existingItem.TenCuaHang = storeProduct.TenCuaHang;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    ProductId = product.MaSP,
                    ProductName = product.TenSP,
                    Price = storeProduct.GiaBan,
                    Quantity = quantity,
                    HinhAnh = product.HinhAnh,

                    IsBulky = laCongKenh,
                    PhiCongKenh = phiCongKenh,
                    CanLapRap = hoTroLapRap,
                    PhiLapRap = phiLapRap,

                    MaTK_Store = storeProduct.MaTK_Store,
                    TenCuaHang = storeProduct.TenCuaHang
                });
            }

            Session[CartSessionKey] = cart;

            return cart.Sum(x => x.Quantity);
        }

        private void AddComboProductToCart(
            int productId,
            int quantity,
            string comboCode,
            string comboName,
            decimal discountPercent)
        {
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var product = db.SANPHAMs.Find(productId);

            if (product == null)
            {
                return;
            }

            var storeProduct = db.Database.SqlQuery<StoreProductCartTemp>(
                @"SELECT TOP 1 
                      CSP.MaTK_Store,
                      CH.TenCH AS TenCuaHang,
                      CSP.GiaBan,
                      CSP.SoLuongTon
                  FROM CUAHANG_SANPHAM CSP
                  INNER JOIN CUAHANG CH ON CSP.MaTK_Store = CH.MaTK
                  WHERE CSP.MaSP = @p0
                    AND CSP.TrangThai = 1
                    AND CSP.SoLuongTon > 0
                  ORDER BY CSP.GiaBan ASC",
                productId
            ).FirstOrDefault();

            if (storeProduct == null)
            {
                return;
            }

            var cart = GetCartFromSession();

            decimal originalPrice = storeProduct.GiaBan;
            decimal discountedPrice = originalPrice - (originalPrice * discountPercent / 100);

            decimal phiCongKenh = ToDecimalSafe(product.PhiCongKenhMacDinh);
            decimal phiLapRap = ToDecimalSafe(product.PhiLapRapMacDinh);

            bool laCongKenh = product.LaCongKenh == true;
            bool hoTroLapRap = product.HoTroLapRap == true;

            var existingItem = cart.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.MaTK_Store == storeProduct.MaTK_Store &&
                x.ComboCode == comboCode
            );

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;

                existingItem.ProductName = product.TenSP;
                existingItem.HinhAnh = product.HinhAnh;
                existingItem.Price = discountedPrice;
                existingItem.OriginalPrice = originalPrice;
                existingItem.DiscountPercent = discountPercent;
                existingItem.IsBulky = laCongKenh;
                existingItem.PhiCongKenh = phiCongKenh;
                existingItem.CanLapRap = hoTroLapRap;
                existingItem.PhiLapRap = phiLapRap;
                existingItem.IsComboItem = true;
                existingItem.ComboCode = comboCode;
                existingItem.ComboName = comboName;
                existingItem.MaTK_Store = storeProduct.MaTK_Store;
                existingItem.TenCuaHang = storeProduct.TenCuaHang;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    ProductId = product.MaSP,
                    ProductName = product.TenSP,
                    Price = discountedPrice,
                    OriginalPrice = originalPrice,
                    Quantity = quantity,
                    HinhAnh = product.HinhAnh,

                    IsBulky = laCongKenh,
                    PhiCongKenh = phiCongKenh,
                    CanLapRap = hoTroLapRap,
                    PhiLapRap = phiLapRap,

                    IsComboItem = true,
                    ComboCode = comboCode,
                    ComboName = comboName,
                    DiscountPercent = discountPercent,

                    MaTK_Store = storeProduct.MaTK_Store,
                    TenCuaHang = storeProduct.TenCuaHang
                });
            }

            Session[CartSessionKey] = cart;
        }

        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1, int? storeId = null)
        {
            var product = db.SANPHAMs.Find(productId);

            if (product == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy sản phẩm."
                    });
                }

                TempData["Error"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index", "Product");
            }

            int totalQty = AddProductToCart(productId, quantity, storeId);

            if (Request.IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    totalItems = totalQty,
                    message = "Đã thêm sản phẩm vào giỏ hàng."
                });
            }

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        public ActionResult BuyNow(
            int? productId,
            string productName,
            decimal? price,
            bool isBulky = false,
            string hinhAnh = "",
            int quantity = 1,
            int? storeId = null)
        {
            if (productId == null)
            {
                TempData["Error"] = "Không lấy được thông tin sản phẩm để mua ngay.";
                return RedirectToAction("Index", "Product");
            }

            var product = db.SANPHAMs.Find(productId.Value);

            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Product");
            }

            AddProductToCart(productId.Value, quantity, storeId);

            return RedirectToAction("Index", "Checkout");
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int productId, int quantity, int? storeId = null, string comboCode = "")
        {
            var cart = GetCartFromSession();

            var item = cart.FirstOrDefault(x =>
                x.ProductId == productId &&
                (storeId == null || x.MaTK_Store == storeId) &&
                ((comboCode ?? "") == "" || x.ComboCode == comboCode)
            );

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }

                Session[CartSessionKey] = cart;
            }

            int totalQty = cart.Sum(x => x.Quantity);
            decimal totalAmount = cart.Sum(x => x.Price * x.Quantity);

            return Json(new
            {
                success = true,
                totalItems = totalQty,
                totalAmount = totalAmount
            });
        }

        [HttpPost]
        public ActionResult RemoveItem(int productId, int? storeId = null, string comboCode = "")
        {
            var cart = GetCartFromSession();

            var item = cart.FirstOrDefault(x =>
                x.ProductId == productId &&
                (storeId == null || x.MaTK_Store == storeId) &&
                ((comboCode ?? "") == "" || x.ComboCode == comboCode)
            );

            if (item != null)
            {
                cart.Remove(item);
                Session[CartSessionKey] = cart;
            }

            int totalQty = cart.Sum(x => x.Quantity);
            decimal totalAmount = cart.Sum(x => x.Price * x.Quantity);

            return Json(new
            {
                success = true,
                totalItems = totalQty,
                totalAmount = totalAmount
            });
        }

        [HttpGet]
        public ActionResult GetCartCount()
        {
            var cart = Session[CartSessionKey] as List<CartItemViewModel>;

            int count = cart != null ? cart.Sum(x => x.Quantity) : 0;

            return Json(new
            {
                count = count
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Clear()
        {
            Session[CartSessionKey] = new List<CartItemViewModel>();

            TempData["Success"] = "Đã xóa toàn bộ giỏ hàng.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult AddComboToCart(int product1Id, int product2Id)
        {
            var combo = ComboSuggestionHelper
                .GetInventoryComboSuggestions(db)
                .FirstOrDefault(x =>
                    (x.Product1Id == product1Id && x.Product2Id == product2Id) ||
                    (x.Product1Id == product2Id && x.Product2Id == product1Id)
                );

            if (combo == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Combo không còn hợp lệ hoặc sản phẩm không đủ tồn kho."
                });
            }

            AddComboProductToCart(
                combo.Product1Id,
                1,
                combo.ComboCode,
                combo.ComboName,
                combo.DiscountPercent
            );

            AddComboProductToCart(
                combo.Product2Id,
                1,
                combo.ComboCode,
                combo.ComboName,
                combo.DiscountPercent
            );

            var cart = GetCartFromSession();

            decimal totalAmount = cart.Sum(x => x.Price * x.Quantity);

            return Json(new
            {
                success = true,
                totalItems = cart.Sum(x => x.Quantity),
                totalAmount = totalAmount,
                message = "Đã thêm combo vào giỏ hàng và áp dụng giảm " + combo.DiscountPercent.ToString("0") + "%."
            });
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

    public class StoreProductCartTemp
    {
        public int MaTK_Store { get; set; }

        public string TenCuaHang { get; set; }

        public decimal GiaBan { get; set; }

        public int SoLuongTon { get; set; }
    }
}