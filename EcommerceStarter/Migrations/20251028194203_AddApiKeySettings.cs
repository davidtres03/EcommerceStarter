using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UspsUserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UspsPasswordEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UspsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpsClientId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpsClientSecretEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpsAccountNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UpsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    FedExAccountNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FedExMeterNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FedExKeyEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FedExPasswordEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FedExEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeySettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeySettings");
        }
    }
}
