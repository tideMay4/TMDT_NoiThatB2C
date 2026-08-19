using System;

namespace DoAnCK.Models.ViewModel
{
    public class ComboSuggestionViewModel
    {
        public string ComboCode { get; set; }

        public string ComboName { get; set; }

        public int Product1Id { get; set; }

        public string Product1Name { get; set; }

        public string Product1Image { get; set; }

        public decimal Product1Price { get; set; }

        public int Product1Stock { get; set; }

        public int Product2Id { get; set; }

        public string Product2Name { get; set; }

        public string Product2Image { get; set; }

        public decimal Product2Price { get; set; }

        public int Product2Stock { get; set; }

        public decimal TotalPrice
        {
            get
            {
                return Product1Price + Product2Price;
            }
        }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount
        {
            get
            {
                return TotalPrice * DiscountPercent / 100;
            }
        }

        public decimal FinalPrice
        {
            get
            {
                return TotalPrice - DiscountAmount;
            }
        }

        public int ComboStock
        {
            get
            {
                return Math.Min(Product1Stock, Product2Stock);
            }
        }
    }
}