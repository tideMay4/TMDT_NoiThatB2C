using System.Collections.Generic;

namespace TMDT_NoiThatB2C.Models
{
    // Class chứa thông tin từng sản phẩm trong giỏ hàng
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public bool IsBulky { get; set; }

        // MỚI THÊM: Biến lưu đường dẫn ảnh
        public string HinhAnh { get; set; }
    }

    // Class tổng chứa cả danh sách sản phẩm và các loại phí
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public decimal ShippingFee { get; set; }
        public decimal BulkyFeePerItem { get; set; }
    }
}