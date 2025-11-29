namespace EcommerceStarter.Models
{
    public class SubCategory
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public string IconClass { get; set; } = "bi-tag-fill"; // Bootstrap icon class
        
        public bool IsEnabled { get; set; } = true;
        
        public int DisplayOrder { get; set; } = 0;
        
        public int CategoryId { get; set; }
        
        public Category Category { get; set; } = null!;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
