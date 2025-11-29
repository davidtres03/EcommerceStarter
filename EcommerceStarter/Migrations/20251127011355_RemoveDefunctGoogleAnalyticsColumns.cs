using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDefunctGoogleAnalyticsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe removal - only drop if columns exist
            // These columns were marked for deletion in AddCustomerAddressFields migration
            // but that migration failed, so we handle it safely here
            
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'SiteSettings' 
                    AND COLUMN_NAME = 'EnableGoogleAnalytics'
                )
                BEGIN
                    ALTER TABLE SiteSettings DROP COLUMN EnableGoogleAnalytics;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'SiteSettings' 
                    AND COLUMN_NAME = 'GoogleAnalyticsTag'
                )
                BEGIN
                    ALTER TABLE SiteSettings DROP COLUMN GoogleAnalyticsTag;
                END
            ");

            // Ensure MeasurementPath column exists (should be added already)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'SiteSettings' 
                    AND COLUMN_NAME = 'MeasurementPath'
                )
                BEGIN
                    ALTER TABLE SiteSettings ADD MeasurementPath nvarchar(100) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore old columns if rolling back
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'SiteSettings' 
                    AND COLUMN_NAME = 'EnableGoogleAnalytics'
                )
                BEGIN
                    ALTER TABLE SiteSettings ADD EnableGoogleAnalytics bit NOT NULL DEFAULT 0;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'SiteSettings' 
                    AND COLUMN_NAME = 'GoogleAnalyticsTag'
                )
                BEGIN
                    ALTER TABLE SiteSettings ADD GoogleAnalyticsTag nvarchar(max) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'SiteSettings' 
                    AND COLUMN_NAME = 'MeasurementPath'
                )
                BEGIN
                    ALTER TABLE SiteSettings DROP COLUMN MeasurementPath;
                END
            ");
        }
    }
}
