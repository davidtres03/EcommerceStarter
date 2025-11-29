using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneIdToSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Central Standard Time");

            // Auto-detect system timezone and update existing records
            migrationBuilder.Sql($@"
                UPDATE SiteSettings 
                SET TimeZoneId = '{TimeZoneInfo.Local.Id}'
                WHERE TimeZoneId = 'Central Standard Time'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "SiteSettings");
        }
    }
}
