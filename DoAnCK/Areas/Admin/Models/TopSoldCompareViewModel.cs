using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class TopSoldCompareViewModel
    {
        public string TenSanPham { get; set; }
        public int TongDaBan { get; set; }
        public string TenCuaHangA { get; set; }
        public int SoLuongShopA { get; set; }
        public double TiLeShopA { get; set; } // % để vẽ thanh progress

        public string TenCuaHangB { get; set; }
        public int SoLuongShopB { get; set; }
        public double TiLeShopB { get; set; }
    }
}