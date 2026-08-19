using System.Collections.Generic;

namespace DoAnCK.Models
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        public decimal SubTotal { get; set; }

        public decimal ShippingFee { get; set; }

        public decimal BulkyFee { get; set; }

        public decimal AssemblyFee { get; set; }

        public decimal TotalFee { get; set; }

        public decimal GrandTotal { get; set; }

        public bool YeuCauLapRap { get; set; }
    }
}