using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MDT_NoiThatB2C.Models
{
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
}

