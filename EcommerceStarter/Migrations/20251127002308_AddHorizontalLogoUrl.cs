using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddHorizontalLogoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add VisitorSessions columns
            migrationBuilder.AddColumn<string>(
                name: "BotName",
                table: "VisitorSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrowserVersion",
                table: "VisitorSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceBrand",
                table: "VisitorSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceModel",
                table: "VisitorSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBot",
                table: "VisitorSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OSVersion",
                table: "VisitorSessions",
                type: "nvarchar(max)",
                nullable: true);

            // Add only NEW SiteSettings columns (not already in 20251124211957_AddHeroSectionCustomization)
            migrationBuilder.AddColumn<int>(
                name: "ButtonStyle",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CardStyle",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CornerRounding",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HorizontalLogoUrl",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NavigationStyle",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SpacingDensity",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove VisitorSessions columns
            migrationBuilder.DropColumn(
                name: "BotName",
                table: "VisitorSessions");

            migrationBuilder.DropColumn(
                name: "BrowserVersion",
                table: "VisitorSessions");

            migrationBuilder.DropColumn(
                name: "DeviceBrand",
                table: "VisitorSessions");

            migrationBuilder.DropColumn(
                name: "DeviceModel",
                table: "VisitorSessions");

            migrationBuilder.DropColumn(
                name: "IsBot",
                table: "VisitorSessions");

            migrationBuilder.DropColumn(
                name: "OSVersion",
                table: "VisitorSessions");

            // Remove only NEW SiteSettings columns
            migrationBuilder.DropColumn(
                name: "ButtonStyle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CardStyle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CornerRounding",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HorizontalLogoUrl",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "NavigationStyle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SpacingDensity",
                table: "SiteSettings");
        }
    }
}
