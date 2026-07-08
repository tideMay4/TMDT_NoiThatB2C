using System;

namespace DoAnCK.Areas.Admin.Models
{
    public class ProductPriceViewModel
    {
        public int MaGia { get; set; }

        public int MaSP { get; set; }

        public string TenSP { get; set; }

        public string HinhAnh { get; set; }

        public string Slug { get; set; }

        public decimal GiaCu { get; set; }

        public decimal GiaMoi { get; set; }

        public DateTime NgayBatDau { get; set; }

        public DateTime? NgayKetThuc { get; set; }

        public string LyDoThayDoi { get; set; }

        public string TrangThai { get; set; }

        public DateTime NgayTao { get; set; }
    }
}