using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TMDT_NoiThatB2C.Models;

namespace TMDT_NoiThatB2C.Controllers
{
    public class CartController : Controller
    {
        //private const string CartSessionKey = "ShoppingCartSession";

        //// Hàm hỗ trợ: Lấy giỏ hàng từ Session
        //private List<CartItemViewModel> GetCartFromSession()
        //{
        //    var cart = Session[CartSessionKey] as List<CartItemViewModel>;
        //    if (cart == null)
        //    {
        //        cart = new List<CartItemViewModel>();
        //        Session[CartSessionKey] = cart;
        //    }
        //    return cart;
        //}

        //// 1. Hiển thị trang Giỏ hàng động
        //public ActionResult Index()
        //{
        //    var cart = GetCartFromSession();
        //    return View(cart); // Truyền dữ liệu thực tế từ Session ra View
        //}

        //// 2. Nút "Thêm vào giỏ hàng" sẽ gọi hàm này qua AJAX
        //[HttpPost]
        //public ActionResult AddToCart(int productId, string productName, decimal price, bool isBulky)
        //{
        //    var cart = GetCartFromSession();
        //    var existingItem = cart.FirstOrDefault(m => m.ProductId == productId);

        //    if (existingItem != null)
        //    {
        //        // Nếu đã có trong giỏ thì tăng số lượng
        //        existingItem.Quantity++;
        //    }
        //    else
        //    {
        //        // Nếu chưa có thì thêm mới
        //        cart.Add(new CartItemViewModel
        //        {
        //            ProductId = productId,
        //            ProductName = productName,
        //            Price = price,
        //            Quantity = 1,
        //            IsBulky = isBulky
        //        });
        //    }

        //    // Lưu lại vào Session
        //    Session[CartSessionKey] = cart;

        //    // Trả về tổng số lượng để update cái icon trên góc phải
        //    int totalQty = cart.Sum(item => item.Quantity);
        //    return Json(new { success = true, totalItems = totalQty });
        //}

        //// 3. Hàm lấy số lượng giỏ hàng (để load lúc mới mở web)
        //[HttpGet]
        //public ActionResult GetCartCount()
        //{
        //    var cart = Session[CartSessionKey] as List<CartItemViewModel>;
        //    int count = cart != null ? cart.Sum(c => c.Quantity) : 0;
        //    return Json(new { count = count }, JsonRequestBehavior.AllowGet);
        //}

        //// 4. Xóa sản phẩm khỏi giỏ
        //[HttpPost]
        //public ActionResult RemoveItem(int productId)
        //{
        //    var cart = GetCartFromSession();
        //    var item = cart.FirstOrDefault(m => m.ProductId == productId);
        //    if (item != null)
        //    {
        //        cart.Remove(item);
        //        Session[CartSessionKey] = cart;
        //    }
        //    int totalQty = cart.Sum(c => c.Quantity);
        //    return Json(new { success = true, totalItems = totalQty });
        //}

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

        // Cập nhật tham số nhận thêm 'hinhAnh'
        [HttpPost]
        public ActionResult AddToCart(int productId, string productName, decimal price, bool isBulky, string hinhAnh)
        {
            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(m => m.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    ProductId = productId,
                    ProductName = productName,
                    Price = price,
                    Quantity = 1,
                    IsBulky = isBulky,
                    HinhAnh = hinhAnh // Lưu ảnh vào giỏ
                });
            }

            Session[CartSessionKey] = cart;
            int totalQty = cart.Sum(item => item.Quantity);
            return Json(new { success = true, totalItems = totalQty });
        }

        // VIẾT CODE LƯU SỐ LƯỢNG KHI BẤM DẤU CỘNG TRỪ
        [HttpPost]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(m => m.ProductId == productId);

            if (item != null)
            {
                item.Quantity = quantity; // Cập nhật số lượng mới
                Session[CartSessionKey] = cart; // Lưu lại vào Session
            }

            return Json(new { success = true });
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
            return Json(new { success = true, totalItems = totalQty });
        }

        [HttpGet]
        public ActionResult GetCartCount()
        {
            var cart = Session[CartSessionKey] as List<CartItemViewModel>;
            int count = cart != null ? cart.Sum(c => c.Quantity) : 0;
            return Json(new { count = count }, JsonRequestBehavior.AllowGet);
        }
    }
}