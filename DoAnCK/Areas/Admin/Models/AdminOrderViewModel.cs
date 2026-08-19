using System;
using System.Collections.Generic;

namespace DoAnCK.Areas.Admin.Models
{
    public class AdminOrderListItemViewModel
    {
        public int MaDH { get; set; }

        public int? MaKH { get; set; }

        public DateTime? NgayDat { get; set; }

        public string TenKhachHang { get; set; }

        public string Email { get; set; }

        public string SDT { get; set; }

        public string DiaChiGiaoHang { get; set; }

        public decimal? TongTien { get; set; }

        public string TrangThai { get; set; }

        public string SanPhamTomTat { get; set; }
    }

    public class AdminOrderDetailViewModel : AdminOrderListItemViewModel
    {
        public string GhiChu { get; set; }

        public List<AdminOrderDetailItemViewModel> Items { get; set; }

        public AdminOrderDetailViewModel()
        {
            Items = new List<AdminOrderDetailItemViewModel>();
        }
    }

    public class AdminOrderDetailItemViewModel
    {
        public int MaSP { get; set; }

        public string TenSP { get; set; }

        public string HinhAnh { get; set; }

        public int? SoLuong { get; set; }

        public decimal? GiaBan { get; set; }

        public decimal? ThanhTien { get; set; }
    }
}