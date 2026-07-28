using System.Collections.Generic;

namespace DoAnCK.Models
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }

        public decimal ShippingFee { get; set; }

        public decimal BulkyFeePerItem { get; set; }

        public CheckoutViewModel()
        {
            CartItems = new List<CartItemViewModel>();
        }
    }
}