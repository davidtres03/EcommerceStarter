using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeConfigTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StripeConfigurationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WasTestMode = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeConfigurationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StripeConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EncryptedPublishableKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedSecretKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedWebhookSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsTestMode = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeConfigurations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StripeConfigurationAuditLogs");

            migrationBuilder.DropTable(
                name: "StripeConfigurations");
        }
    }
}
