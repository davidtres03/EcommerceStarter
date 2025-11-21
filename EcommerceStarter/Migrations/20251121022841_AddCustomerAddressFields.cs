using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableGoogleAnalytics",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "GoogleAnalyticsTag",
                table: "SiteSettings");

            migrationBuilder.AddColumn<string>(
                name: "MeasurementPath",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RefundHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    StripeRefundId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RefundReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RefundNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InventoryRestocked = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefundStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundHistories_OrderId",
                table: "RefundHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundHistories_ProcessedDate",
                table: "RefundHistories",
                column: "ProcessedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundHistories");

            migrationBuilder.DropColumn(
                name: "MeasurementPath",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundedDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "City",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "AspNetUsers");

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
