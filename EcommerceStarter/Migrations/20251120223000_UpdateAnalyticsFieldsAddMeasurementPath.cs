using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnalyticsFieldsAddMeasurementPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove deprecated analytics fields
            migrationBuilder.DropColumn(
                name: "EnableGoogleAnalytics",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "GoogleAnalyticsTag",
                table: "SiteSettings");

            // Add MeasurementPath for Cloudflare Gateway
            migrationBuilder.AddColumn<string>(
                name: "MeasurementPath",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "/metrics");

            // Update existing records to have the default measurement path
            migrationBuilder.Sql(
                "UPDATE SiteSettings SET MeasurementPath = '/metrics' WHERE MeasurementPath IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove MeasurementPath
            migrationBuilder.DropColumn(
                name: "MeasurementPath",
                table: "SiteSettings");

            // Restore deprecated fields
            migrationBuilder.AddColumn<bool>(
                name: "EnableGoogleAnalytics",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GoogleAnalyticsTag",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
