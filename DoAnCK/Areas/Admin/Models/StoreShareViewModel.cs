using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class StoreShareViewModel
    {
        public string TenCuaHang { get; set; }
        public int SoLuongDaBan { get; set; }
        public double TiLePhanTram { get; set; }
        public string ColorHex { get; set; } // Dải màu Nâu - Kem
    }
}