using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        // Random, unique order number for customer-facing use
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;
        
        // Nullable for guest orders
        public string? UserId { get; set; }
        
        // Store email for all orders (guests and registered users)
        [MaxLength(255)]
        public string CustomerEmail { get; set; } = string.Empty;
        
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal Subtotal { get; set; }
        
        // TAX FUNCTIONALITY - Currently disabled by default
        // Uncomment and configure tax rates in SiteSettings or use the Setup Wizard
        // to enable sales tax calculation for your store
        public decimal TaxAmount { get; set; } = 0m;  // Set to 0 by default - no tax
        
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        
        public string ShippingName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingZip { get; set; } = string.Empty;
        
        // Tracking number for shipped orders
        [MaxLength(100)]
        public string? TrackingNumber { get; set; }
        
        // Courier/carrier used for this shipment
        public Courier TrackingCourier { get; set; } = Courier.Unknown;
        
        // Payment information
        public string? PaymentIntentId { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        
        // Refund information
        public decimal? RefundedAmount { get; set; }
        public DateTime? RefundedDate { get; set; }
        
        // Helper property to identify guest orders
        public bool IsGuestOrder => string.IsNullOrEmpty(UserId);
        
        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<RefundHistory> RefundHistories { get; set; } = new List<RefundHistory>();
    }
    
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }
    
    public enum PaymentStatus
    {
        Pending,
        Succeeded,
        Failed,
        Refunded
    }
}
