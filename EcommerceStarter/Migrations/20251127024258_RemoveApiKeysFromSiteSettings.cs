using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class RemoveApiKeysFromSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrevoApiKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EnableInternalServiceAuth",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "InternalServiceKeyEncrypted",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ResendApiKey",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SendGridApiKey",
                table: "SiteSettings");

            migrationBuilder.AddColumn<int>(
                name: "ApiConfigurationId",
                table: "SiteSettings",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiConfigurationId",
                table: "SiteSettings");

            migrationBuilder.AddColumn<string>(
                name: "BrevoApiKey",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableInternalServiceAuth",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "InternalServiceKeyEncrypted",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResendApiKey",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SendGridApiKey",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
