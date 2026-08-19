namespace DoAnCK.Models
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public string HinhAnh { get; set; }

        public bool IsBulky { get; set; }

        public decimal PhiCongKenh { get; set; }

        public bool CanLapRap { get; set; }

        public decimal PhiLapRap { get; set; }

        public int? MaTK_Store { get; set; }

        public string TenCuaHang { get; set; }

        public bool IsComboItem { get; set; }

        public string ComboCode { get; set; }

        public string ComboName { get; set; }

        public decimal OriginalPrice { get; set; }

        public decimal DiscountPercent { get; set; }
    }
}