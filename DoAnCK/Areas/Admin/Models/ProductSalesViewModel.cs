using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class ProductSalesViewModel
    {
        public int TongSanPhamBanRaThangNay { get; set; }
        public double PhanTramTangTruong { get; set; }
        public bool IsTangTruongDuong { get; set; }
    }
}