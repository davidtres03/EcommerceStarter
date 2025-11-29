using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddAIConfigurationToApiKeySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AIEnableFallback",
                table: "ApiKeySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AIMaxCostPerRequest",
                table: "ApiKeySettings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AIPreferredBackend",
                table: "ApiKeySettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaudeApiKeyEncrypted",
                table: "ApiKeySettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClaudeEnabled",
                table: "ApiKeySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ClaudeMaxTokens",
                table: "ApiKeySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClaudeModel",
                table: "ApiKeySettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OllamaEnabled",
                table: "ApiKeySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OllamaEndpoint",
                table: "ApiKeySettings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OllamaModel",
                table: "ApiKeySettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIEnableFallback",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "AIMaxCostPerRequest",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "AIPreferredBackend",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "ClaudeApiKeyEncrypted",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "ClaudeEnabled",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "ClaudeMaxTokens",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "ClaudeModel",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "OllamaEnabled",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "OllamaEndpoint",
                table: "ApiKeySettings");

            migrationBuilder.DropColumn(
                name: "OllamaModel",
                table: "ApiKeySettings");
        }
    }
}
