namespace EcommerceStarter.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int StockQuantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }
}
