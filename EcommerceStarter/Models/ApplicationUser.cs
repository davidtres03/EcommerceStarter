using Microsoft.AspNetCore.Identity;

namespace EcommerceStarter.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Customer address information
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        
        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<CustomerAuditLog> AuditLogs { get; set; } = new List<CustomerAuditLog>();
    }
}
