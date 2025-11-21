using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    public class RefundHistory
    {
        public int Id { get; set; }
        
        public int OrderId { get; set; }
        
        [MaxLength(100)]
        public string StripeRefundId { get; set; } = string.Empty;
        
        public decimal RefundAmount { get; set; }
        
        [MaxLength(50)]
        public string RefundType { get; set; } = string.Empty; // "full" or "partial"
        
        [MaxLength(100)]
        public string RefundReason { get; set; } = string.Empty; // defective, wrong_item, customer_request, other
        
        [MaxLength(500)]
        public string? RefundNotes { get; set; }
        
        public bool InventoryRestocked { get; set; }
        
        [MaxLength(255)]
        public string ProcessedBy { get; set; } = string.Empty; // Admin username
        
        public DateTime ProcessedDate { get; set; } = DateTime.UtcNow;
        
        [MaxLength(50)]
        public string RefundStatus { get; set; } = "succeeded"; // succeeded, failed, pending, cancelled
        
        // Navigation property
        public virtual Order? Order { get; set; }
    }
}
