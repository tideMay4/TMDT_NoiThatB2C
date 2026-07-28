namespace DoAnCK.Models
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public bool IsBulky { get; set; }

        public string HinhAnh { get; set; }

        public decimal Total
        {
            get
            {
                return Price * Quantity;
            }
        }
    }
}