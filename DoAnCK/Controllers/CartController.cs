using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAnCK.Models;

namespace DoAnCK.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "ShoppingCartSession";

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
            return View(cart);
        }

        private int AddItemToCart(
            int productId,
            string productName,
            decimal price,
            bool isBulky,
            string hinhAnh,
            int quantity)
        {
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var cart = GetCartFromSession();

            var existingItem = cart.FirstOrDefault(m => m.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    ProductId = productId,
                    ProductName = productName,
                    Price = price,
                    Quantity = quantity,
                    IsBulky = isBulky,
                    HinhAnh = hinhAnh
                });
            }

            Session[CartSessionKey] = cart;

            int totalQty = cart.Sum(item => item.Quantity);

            return totalQty;
        }

        [HttpPost]
        public ActionResult AddToCart(
    int? productId,
    string productName,
    decimal? price,
    bool isBulky = false,
    string hinhAnh = "",
    int quantity = 1)
        {
            if (productId == null || price == null)
            {
                TempData["Error"] = "Không lấy được thông tin sản phẩm để thêm vào giỏ hàng.";

                if (Request.UrlReferrer != null)
                {
                    return Redirect(Request.UrlReferrer.ToString());
                }

                return RedirectToAction("Index", "Product");
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                productName = "Sản phẩm";
            }

            int totalQty = AddItemToCart(
                productId.Value,
                productName,
                price.Value,
                isBulky,
                hinhAnh,
                quantity
            );

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

            if (Request.UrlReferrer != null)
            {
                return Redirect(Request.UrlReferrer.ToString());
            }

            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        public ActionResult BuyNow(
            int? productId,
            string productName,
            decimal? price,
            bool isBulky = false,
            string hinhAnh = "",
            int quantity = 1)
        {
            if (productId == null || price == null)
            {
                TempData["Error"] = "Không lấy được thông tin sản phẩm để mua ngay.";
                return RedirectToAction("Index", "Product");
            }

            if (string.IsNullOrWhiteSpace(productName))
            {
                productName = "Sản phẩm";
            }

            AddItemToCart(
                productId.Value,
                productName,
                price.Value,
                isBulky,
                hinhAnh,
                quantity
            );

            return RedirectToAction("Index", "Checkout");
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCartFromSession();

            var item = cart.FirstOrDefault(m => m.ProductId == productId);

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

            int totalQty = cart.Sum(c => c.Quantity);
            decimal totalAmount = cart.Sum(c => c.Price * c.Quantity);

            return Json(new
            {
                success = true,
                totalItems = totalQty,
                totalAmount = totalAmount
            });
        }

        [HttpPost]
        public ActionResult RemoveItem(int productId)
        {
            var cart = GetCartFromSession();

            var item = cart.FirstOrDefault(m => m.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                Session[CartSessionKey] = cart;
            }

            int totalQty = cart.Sum(c => c.Quantity);
            decimal totalAmount = cart.Sum(c => c.Price * c.Quantity);

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

            int count = cart != null ? cart.Sum(c => c.Quantity) : 0;

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
    }
}