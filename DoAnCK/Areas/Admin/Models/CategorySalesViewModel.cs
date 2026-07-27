using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class CategorySalesViewModel
    {
        public string TenDanhMuc { get; set; }
        public int SoLuongBan { get; set; }
        public double TyLePhanTram { get; set; }
        public string ColorClass { get; set; } // Dùng để lưu màu CSS ngẫu nhiên hoặc theo thứ hạng
    }
}