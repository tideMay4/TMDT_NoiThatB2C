using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class RevenueViewModel
    {
        public decimal TongDoanhThuThangNay { get; set; }
        public double PhanTramTangTruong { get; set; }
        public bool IsTangTruongDuong { get; set; } // Để xác định hiển thị mũi tên lên (xanh) hay xuống (đỏ)
    }
}