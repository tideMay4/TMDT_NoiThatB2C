using System;

namespace DoAnCK.Areas.Admin.Models
{
    public class CategoryViewModel
    {
        public int MaDM { get; set; }

        public string TenDM { get; set; }

        public string Slug { get; set; }

        public string MoTa { get; set; }

        public string HinhAnh { get; set; }

        public int SoSanPham { get; set; }

        public bool TrangThai { get; set; }

        public DateTime NgayTao { get; set; }
    }
}