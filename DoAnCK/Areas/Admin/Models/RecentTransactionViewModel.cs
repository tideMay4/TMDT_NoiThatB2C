using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class RecentTransactionViewModel
    {
        public string MaDonHang { get; set; }
        public string TenKhachHang { get; set; }
        public string EmailKhachHang { get; set; }
        public string TenCuaHang { get; set; }
        public string TenSanPham { get; set; }
        public DateTime NgayDat { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
        public string BadgeClass { get; set; }
    }
}