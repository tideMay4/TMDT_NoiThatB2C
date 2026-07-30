using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class StoreCompareItem
    {
        public string TenCuaHang { get; set; }
        public int LuotXem { get; set; }
    }

    public class ProductCompareViewItem
    {
        public string TenSanPham { get; set; }
        public List<StoreCompareItem> DanhSachCuaHang { get; set; }
    }
}