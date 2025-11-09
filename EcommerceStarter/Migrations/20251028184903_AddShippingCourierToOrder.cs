using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingCourierToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShippingCourier",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingCourier",
                table: "Orders");
        }
    }
}
