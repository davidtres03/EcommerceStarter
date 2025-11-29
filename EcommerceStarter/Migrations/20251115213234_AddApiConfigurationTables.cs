using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddApiConfigurationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicId",
                table: "Products",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApiConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsTestMode = table.Column<bool>(type: "bit", nullable: false),
                    EncryptedValue1 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedValue2 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedValue3 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedValue4 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedValue5 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastValidated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiConfigurationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApiConfigurationId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Changes = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TestStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiConfigurationAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiConfigurationAuditLogs_ApiConfigurations_ApiConfigurationId",
                        column: x => x.ApiConfigurationId,
                        principalTable: "ApiConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurationAuditLogs_Action",
                table: "ApiConfigurationAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurationAuditLogs_ApiConfigurationId",
                table: "ApiConfigurationAuditLogs",
                column: "ApiConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurationAuditLogs_Timestamp",
                table: "ApiConfigurationAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurationAuditLogs_UserId",
                table: "ApiConfigurationAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_ApiType",
                table: "ApiConfigurations",
                column: "ApiType");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_ApiType_Name",
                table: "ApiConfigurations",
                columns: new[] { "ApiType", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_IsActive",
                table: "ApiConfigurations",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiConfigurationAuditLogs");

            migrationBuilder.DropTable(
                name: "ApiConfigurations");

            migrationBuilder.DropColumn(
                name: "CloudinaryPublicId",
                table: "Products");
        }
    }
}
