using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, update any empty OrderNumbers with unique values
            migrationBuilder.Sql(@"
                UPDATE Orders 
                SET OrderNumber = 'ORD-' + CAST(NEWID() AS NVARCHAR(36))
                WHERE OrderNumber = '' OR OrderNumber IS NULL
            ");

            // Truncate any data that exceeds new limits
            migrationBuilder.Sql(@"
                -- Truncate Products.ImageUrl if needed
                UPDATE Products
                SET ImageUrl = SUBSTRING(ImageUrl, 1, 500)
                WHERE LEN(ImageUrl) > 500;

                -- Truncate Products.Name if needed
                UPDATE Products
                SET Name = SUBSTRING(Name, 1, 200)
                WHERE LEN(Name) > 200;

                -- Truncate Products.Category if needed
                UPDATE Products
                SET Category = SUBSTRING(Category, 1, 100)
                WHERE LEN(Category) > 100;

                -- Truncate Products.SubCategory if needed
                UPDATE Products
                SET SubCategory = SUBSTRING(SubCategory, 1, 100)
                WHERE LEN(SubCategory) > 100;

                -- Truncate Orders.CustomerEmail if needed
                UPDATE Orders
                SET CustomerEmail = SUBSTRING(CustomerEmail, 1, 255)
                WHERE LEN(CustomerEmail) > 255;
            ");

            // Alter string columns to have max length for indexing
            migrationBuilder.AlterColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SubCategory",
                table: "Products",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Add unique index on Orders.OrderNumber for fast lookup
            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            // Add index on Orders.CustomerEmail for guest order lookup
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerEmail",
                table: "Orders",
                column: "CustomerEmail");

            // Add index on Products.Category for filtering
            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            // Add index on Products.SubCategory for filtering
            migrationBuilder.CreateIndex(
                name: "IX_Products_SubCategory",
                table: "Products",
                column: "SubCategory");

            // Add composite index on Orders for date-based queries
            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderDate_Status",
                table: "Orders",
                columns: new[] { "OrderDate", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerEmail",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Products_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SubCategory",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderDate_Status",
                table: "Orders");

            // Revert column changes
            migrationBuilder.AlterColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerEmail",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "SubCategory",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}
