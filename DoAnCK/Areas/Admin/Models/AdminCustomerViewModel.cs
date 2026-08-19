using System;

namespace DoAnCK.Areas.Admin.Models
{
    public class AdminCustomerViewModel
    {
        public int MaKH { get; set; }

        public int? MaTK { get; set; }

        public string HoTen { get; set; }

        public string Email { get; set; }

        public string SDT { get; set; }

        public string DiaChi { get; set; }

        public DateTime? NgayDangKy { get; set; }

        public DateTime? NgayTaoTaiKhoan { get; set; }

        public bool TrangThai { get; set; }

        public int SoDonHang { get; set; }

        public decimal TongChiTieu { get; set; }

        public DateTime? LanMuaGanNhat { get; set; }
    }
}