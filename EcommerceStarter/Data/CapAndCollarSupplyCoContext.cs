using System;
using System.Collections.Generic;
using EcommerceStarter.Models.Generated;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Data;

public partial class EcommerceStarterContext : DbContext
{
    public EcommerceStarterContext()
    {
    }

    public EcommerceStarterContext(DbContextOptions<EcommerceStarterContext> options)
        : base(options)
    {
    }

    public virtual DbSet<SiteSetting> SiteSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.Property(e => e.AccentColor).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.AdminNotificationEmail).HasMaxLength(200);
            entity.Property(e => e.BrevoApiKey).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.ContactEmail).HasMaxLength(200);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.EmailButtonColor)
                .HasMaxLength(20)
                .HasDefaultValue("");
            entity.Property(e => e.EmailFooterText).HasMaxLength(1000);
            entity.Property(e => e.EmailFromAddress).HasMaxLength(200);
            entity.Property(e => e.EmailFromName).HasMaxLength(200);
            entity.Property(e => e.EmailHeaderColor)
                .HasMaxLength(20)
                .HasDefaultValue("");
            entity.Property(e => e.EmailLogoUrl).HasMaxLength(500);
            entity.Property(e => e.EmailSupportAddress)
                .HasMaxLength(200)
                .HasDefaultValue("");
            entity.Property(e => e.FacebookUrl).HasMaxLength(500);
            entity.Property(e => e.FaviconUrl).HasMaxLength(500);
            entity.Property(e => e.GoogleAnalyticsMeasurementId).HasMaxLength(50);
            entity.Property(e => e.HeadingFont).HasMaxLength(100);
            entity.Property(e => e.HeroImageUrl).HasMaxLength(500);
            entity.Property(e => e.InstagramUrl).HasMaxLength(500);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(200);
            entity.Property(e => e.LinkedInUrl).HasMaxLength(500);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.MetaDescription).HasMaxLength(500);
            entity.Property(e => e.MetaKeywords).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.PrimaryColor).HasMaxLength(20);
            entity.Property(e => e.PrimaryDark).HasMaxLength(20);
            entity.Property(e => e.PrimaryFont).HasMaxLength(100);
            entity.Property(e => e.PrimaryLight).HasMaxLength(20);
            entity.Property(e => e.ResendApiKey).HasMaxLength(500);
            entity.Property(e => e.SecondaryColor).HasMaxLength(20);
            entity.Property(e => e.SendGridApiKey).HasMaxLength(500);
            entity.Property(e => e.SiteIcon).HasMaxLength(50);
            entity.Property(e => e.SiteName).HasMaxLength(100);
            entity.Property(e => e.SiteTagline).HasMaxLength(200);
            entity.Property(e => e.SmtpHost).HasMaxLength(200);
            entity.Property(e => e.SmtpPassword).HasMaxLength(500);
            entity.Property(e => e.SmtpUsername).HasMaxLength(200);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.SupportEmail).HasMaxLength(200);
            entity.Property(e => e.TwitterUrl).HasMaxLength(500);
            entity.Property(e => e.YouTubeUrl).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
