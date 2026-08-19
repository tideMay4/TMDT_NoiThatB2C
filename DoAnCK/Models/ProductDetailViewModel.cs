using System;
using System.Collections.Generic;

namespace DoAnCK.Models
{
    public class ProductDetailViewModel
    {
        public SANPHAM Product { get; set; }

        public int SoLuotMua { get; set; }

        public int TongDanhGia { get; set; }

        public double DiemTrungBinh { get; set; }

        public bool DaMuaSanPham { get; set; }

        public bool DaDangNhap { get; set; }

        public bool DaYeuThich { get; set; }

        public bool DaDanhGia { get; set; }

        public List<ProductReviewViewModel> Reviews { get; set; }

        public List<SANPHAM> RelatedProducts { get; set; }

        public ProductDetailViewModel()
        {
            Reviews = new List<ProductReviewViewModel>();
            RelatedProducts = new List<SANPHAM>();
        }
    }

    public class ProductReviewViewModel
    {
        public int MaDG { get; set; }

        public int MaSP { get; set; }

        public int? MaKH { get; set; }

        public string TenKhachHang { get; set; }

        public int SoSao { get; set; }

        public string BinhLuan { get; set; }

        public DateTime NgayDanhGia { get; set; }
    }
}