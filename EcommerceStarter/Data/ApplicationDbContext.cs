using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Models;
using EcommerceStarter.Models.VisitorTracking;
using EcommerceStarter.Models.AI;
using EcommerceStarter.Models.Service;

namespace EcommerceStarter.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<RefundHistory> RefundHistories { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<StripeConfiguration> StripeConfigurations { get; set; }
        public DbSet<StripeConfigurationAuditLog> StripeConfigurationAuditLogs { get; set; }
        public DbSet<SslConfiguration> SslConfigurations { get; set; }
        public DbSet<SslConfigurationAuditLog> SslConfigurationAuditLogs { get; set; }
        public DbSet<SecurityAuditLog> SecurityAuditLogs { get; set; }
        public DbSet<BlockedIp> BlockedIps { get; set; }
        public DbSet<SecuritySettings> SecuritySettings { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }
        public DbSet<SetupStatus> SetupStatus { get; set; }
        public DbSet<CustomerAuditLog> CustomerAuditLogs { get; set; }
        public DbSet<ApiKeySettings> ApiKeySettings { get; set; }
        public DbSet<ApiConfiguration> ApiConfigurations { get; set; }
        public DbSet<ApiConfigurationAuditLog> ApiConfigurationAuditLogs { get; set; }
        
        // New normalized API configuration tables
        public DbSet<ApiProvider> ApiProviders { get; set; }
        public DbSet<ApiSetting> ApiSettings { get; set; }
        
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<VariantAttribute> VariantAttributes { get; set; }
        public DbSet<VariantAttributeValue> VariantAttributeValues { get; set; }

        // Visitor Tracking Tables
        public DbSet<VisitorSession> VisitorSessions { get; set; }
        public DbSet<PageView> PageViews { get; set; }
        public DbSet<VisitorEvent> VisitorEvents { get; set; }

        // AI System Tables
        public DbSet<AdminAIConfig> AdminAIConfigs { get; set; }
        public DbSet<AIChatHistory> AIChatHistories { get; set; }
        public DbSet<AIModificationLog> AIModificationLogs { get; set; }

        // Service Monitoring Tables
        public DbSet<ServiceStatusLog> ServiceStatusLogs { get; set; }
        public DbSet<UpdateHistory> UpdateHistories { get; set; }
        public DbSet<ServiceErrorLog> ServiceErrorLogs { get; set; }

        // JWT Authentication Tables
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // Image Storage (database-backed to prevent loss on deployment)
        public DbSet<StoredImage> StoredImages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Product
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // Configure Product - Category relationship
            builder.Entity<Product>()
                .HasOne(p => p.CategoryNavigation)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Product - SubCategory relationship
            builder.Entity<Product>()
                .HasOne(p => p.SubCategoryNavigation)
                .WithMany(sc => sc.Products)
                .HasForeignKey(p => p.SubCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure ProductVariant
            builder.Entity<ProductVariant>()
                .HasOne(pv => pv.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductVariant>()
                .Property(pv => pv.PriceOverride)
                .HasPrecision(18, 2);

            builder.Entity<ProductVariant>()
                .HasIndex(pv => pv.ProductId);

            builder.Entity<ProductVariant>()
                .HasIndex(pv => new { pv.ProductId, pv.DisplayOrder });

            // Configure Category
            builder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            // Configure SubCategory
            builder.Entity<SubCategory>()
                .HasOne(sc => sc.Category)
                .WithMany()
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SubCategory>()
                .HasIndex(sc => new { sc.CategoryId, sc.Name })
                .IsUnique();

            // Configure Order
            builder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TaxAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.RefundedAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);

            // Configure RefundHistory
            builder.Entity<RefundHistory>()
                .Property(rh => rh.RefundAmount)
                .HasPrecision(18, 2);

            builder.Entity<RefundHistory>()
                .HasOne(rh => rh.Order)
                .WithMany(o => o.RefundHistories)
                .HasForeignKey(rh => rh.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RefundHistory>()
                .HasIndex(rh => rh.OrderId);

            builder.Entity<RefundHistory>()
                .HasIndex(rh => rh.ProcessedDate);

            // Configure OrderItem
            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            // Configure SecurityAuditLog
            builder.Entity<SecurityAuditLog>()
                .HasIndex(s => s.IpAddress);

            builder.Entity<SecurityAuditLog>()
                .HasIndex(s => s.Timestamp);

            builder.Entity<SecurityAuditLog>()
                .HasIndex(s => s.EventType);

            // Configure BlockedIp
            builder.Entity<BlockedIp>()
                .HasIndex(b => b.IpAddress)
                .IsUnique();

            // Configure CustomerAuditLog
            builder.Entity<CustomerAuditLog>()
                .HasOne(cal => cal.Customer)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(cal => cal.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CustomerAuditLog>()
                .HasIndex(cal => cal.CustomerId);

            builder.Entity<CustomerAuditLog>()
                .HasIndex(cal => cal.CreatedAt);

            builder.Entity<CustomerAuditLog>()
                .HasIndex(cal => cal.Category);

            builder.Entity<CustomerAuditLog>()
                .HasIndex(cal => cal.EventType);

            // Configure VisitorSession
            builder.Entity<VisitorSession>()
                .HasIndex(vs => vs.SessionId)
                .IsUnique();

            builder.Entity<VisitorSession>()
                .HasIndex(vs => vs.StartTime);

            builder.Entity<VisitorSession>()
                .HasIndex(vs => vs.IpAddress);

            builder.Entity<VisitorSession>()
                .HasIndex(vs => vs.UserId);

            // Configure PageView
            builder.Entity<PageView>()
                .HasOne(pv => pv.Session)
                .WithMany(vs => vs.PageViews)
                .HasForeignKey(pv => pv.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PageView>()
                .HasIndex(pv => pv.SessionId);

            builder.Entity<PageView>()
                .HasIndex(pv => pv.Timestamp);

            builder.Entity<PageView>()
                .HasIndex(pv => pv.Url);

            // Configure VisitorEvent
            builder.Entity<VisitorEvent>()
                .HasOne(ve => ve.Session)
                .WithMany(vs => vs.Events)
                .HasForeignKey(ve => ve.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VisitorEvent>()
                .HasIndex(ve => ve.SessionId);

            builder.Entity<VisitorEvent>()
                .HasIndex(ve => ve.Timestamp);

            builder.Entity<VisitorEvent>()
                .HasIndex(ve => ve.Category);

            builder.Entity<VisitorEvent>()
                .HasIndex(ve => ve.Action);

            builder.Entity<VisitorEvent>()
                .Property(ve => ve.Value)
                .HasPrecision(18, 2);

            // Configure SiteSettings
            builder.Entity<SiteSettings>()
                .Property(ss => ss.TaxRate)
                .HasPrecision(5, 2); // Max 999.99%

            // Configure VariantAttribute
            builder.Entity<VariantAttribute>()
                .HasOne(va => va.Product)
                .WithMany(p => p.VariantAttributes)
                .HasForeignKey(va => va.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VariantAttribute>()
                .HasIndex(va => new { va.ProductId, va.DisplayOrder });

            // Configure VariantAttributeValue
            builder.Entity<VariantAttributeValue>()
                .HasOne(vav => vav.ProductVariant)
                .WithMany(pv => pv.AttributeValues)
                .HasForeignKey(vav => vav.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<VariantAttributeValue>()
                .HasOne(vav => vav.VariantAttribute)
                .WithMany()
                .HasForeignKey(vav => vav.VariantAttributeId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<VariantAttributeValue>()
                .HasIndex(vav => new { vav.ProductVariantId, vav.VariantAttributeId });

            // Configure ApiConfiguration
            builder.Entity<ApiConfiguration>()
                .HasIndex(ac => new { ac.ApiType, ac.Name })
                .IsUnique();

            builder.Entity<ApiConfiguration>()
                .HasIndex(ac => ac.ApiType);

            builder.Entity<ApiConfiguration>()
                .HasIndex(ac => ac.IsActive);

            builder.Entity<ApiConfiguration>()
                .HasMany(ac => ac.AuditLogs)
                .WithOne(aal => aal.ApiConfiguration)
                .HasForeignKey(aal => aal.ApiConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ApiConfigurationAuditLog
            builder.Entity<ApiConfigurationAuditLog>()
                .HasIndex(aal => aal.ApiConfigurationId);

            builder.Entity<ApiConfigurationAuditLog>()
                .HasIndex(aal => aal.Timestamp);

            builder.Entity<ApiConfigurationAuditLog>()
                .HasIndex(aal => aal.Action);

            builder.Entity<ApiConfigurationAuditLog>()
                .HasIndex(aal => aal.UserId);

            // Configure ApiKeySettings decimal precision
            builder.Entity<ApiKeySettings>()
                .Property(aks => aks.AIMaxCostPerRequest)
                .HasPrecision(18, 2);

            // Configure ServiceStatusLog decimal precision
            builder.Entity<ServiceStatusLog>()
                .Property(ssl => ssl.CpuUsagePercent)
                .HasPrecision(5, 2);

            builder.Entity<ServiceStatusLog>()
                .Property(ssl => ssl.UptimePercent)
                .HasPrecision(5, 2);
        }
    }
}
