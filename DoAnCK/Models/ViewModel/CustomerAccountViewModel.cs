using System;
using System.Collections.Generic;

namespace DoAnCK.Models.ViewModel
{
    public class CustomerAccountViewModel
    {
        public int MaTK { get; set; }
        public int MaKH { get; set; }

        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SDT { get; set; }
        public string DiaChi { get; set; }

        public DateTime? NgayDangKy { get; set; }
    }

    public class CustomerOrderHistoryViewModel
    {
        public int MaDH { get; set; }
        public DateTime? NgayDat { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public decimal? TongTien { get; set; }
        public string TrangThai { get; set; }
        public string SanPhamTomTat { get; set; }
    }

    public class CustomerReviewViewModel
    {
        public int MaDG { get; set; }
        public int MaSP { get; set; }

        public string TenSP { get; set; }
        public string HinhAnh { get; set; }

        public int? SoSao { get; set; }
        public string NoiDung { get; set; }
        public DateTime? NgayDanhGia { get; set; }
    }
}