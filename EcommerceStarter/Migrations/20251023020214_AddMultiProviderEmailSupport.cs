using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiProviderEmailSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BrevoApiKey",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailProvider",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ResendApiKey",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpHost",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpPassword",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SmtpUseSsl",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUsername",
                table: "SiteSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrevoApiKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailProvider",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ResendApiKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpHost",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPassword",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpUseSsl",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SmtpUsername",
                table: "SiteSettings");
        }
    }
}
