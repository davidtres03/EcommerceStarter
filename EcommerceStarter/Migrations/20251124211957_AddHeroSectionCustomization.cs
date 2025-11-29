using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroSectionCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeroBadgeText",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroFeature1Icon",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroFeature1Text",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroFeature2Icon",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroFeature2Text",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroFeature3Icon",
                table: "SiteSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroFeature3Text",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroPrimaryButtonLink",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroPrimaryButtonText",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeroSecondaryButtonLink",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroSecondaryButtonText",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroSubtitle",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroTitle",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowHeroFeatures",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowScrollIndicator",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeroBadgeText",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroFeature1Icon",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroFeature1Text",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroFeature2Icon",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroFeature2Text",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroFeature3Icon",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroFeature3Text",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroPrimaryButtonLink",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroPrimaryButtonText",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroSecondaryButtonLink",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroSecondaryButtonText",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroSubtitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowHeroFeatures",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowScrollIndicator",
                table: "SiteSettings");
        }
    }
}
