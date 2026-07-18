using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class BestSellerViewModel
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string HinhAnh { get; set; }
        public string TenDanhMuc { get; set; }
        public decimal GiaBan { get; set; }
        public int TongDaBan { get; set; }
    }
}