using System;

namespace DoAnCK.Areas.Admin.Models
{
    public class ProductViewModel
    {
        public int MaSP { get; set; }

        public string TenSP { get; set; }

        public int MaDM { get; set; }

        public string DanhMuc { get; set; }

        public string HinhAnh { get; set; }

        public decimal Gia { get; set; }

        public decimal? GiaCu { get; set; }

        public int TonKho { get; set; }

        public string Slug { get; set; }

        public string MoTa { get; set; }

        public string ThuongHieu { get; set; }

        public string BaoHanh { get; set; }

        public decimal VAT { get; set; }

        public string MetaTitle { get; set; }

        public string MetaDescription { get; set; }

        public string MetaKeyword { get; set; }

        public bool TrangThai { get; set; }

        public bool NoiBat { get; set; }

        public DateTime NgayTao { get; set; }
    }
}