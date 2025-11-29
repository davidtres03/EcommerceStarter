using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoBlacklistSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPermanentBlacklistEnabled",
                table: "SecuritySettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ErrorSpikeConsecutiveMinutes",
                table: "SecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ErrorSpikeThresholdPerMinute",
                table: "SecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginBurstThreshold",
                table: "SecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginBurstWindowMinutes",
                table: "SecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReblockCountThreshold",
                table: "SecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReblockWindowHours",
                table: "SecuritySettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPermanentBlacklistEnabled",
                table: "SecuritySettings");

            migrationBuilder.DropColumn(
                name: "ErrorSpikeConsecutiveMinutes",
                table: "SecuritySettings");

            migrationBuilder.DropColumn(
                name: "ErrorSpikeThresholdPerMinute",
                table: "SecuritySettings");

            migrationBuilder.DropColumn(
                name: "FailedLoginBurstThreshold",
                table: "SecuritySettings");

            migrationBuilder.DropColumn(
                name: "FailedLoginBurstWindowMinutes",
                table: "SecuritySettings");

            migrationBuilder.DropColumn(
                name: "ReblockCountThreshold",
                table: "SecuritySettings");

            migrationBuilder.DropColumn(
                name: "ReblockWindowHours",
                table: "SecuritySettings");
        }
    }
}
