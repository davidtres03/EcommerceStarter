using Microsoft.AspNetCore.Identity;

namespace EcommerceStarter.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<CustomerAuditLog> AuditLogs { get; set; } = new List<CustomerAuditLog>();
    }
}
