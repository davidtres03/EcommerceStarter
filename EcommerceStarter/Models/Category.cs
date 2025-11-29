namespace EcommerceStarter.Models
{
    public class Category
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public string IconClass { get; set; } = "bi-tag"; // Bootstrap icon class (e.g., bi-tag, bi-shirt, bi-basket)
        
        public bool IsEnabled { get; set; } = true;
        
        public int DisplayOrder { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation property
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
