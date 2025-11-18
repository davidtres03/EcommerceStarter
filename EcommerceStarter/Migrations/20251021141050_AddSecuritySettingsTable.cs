using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddSecuritySettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecuritySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaxRequestsPerMinute = table.Column<int>(type: "int", nullable: false),
                    MaxRequestsPerSecond = table.Column<int>(type: "int", nullable: false),
                    MaxRequestsPerMinuteAuth = table.Column<int>(type: "int", nullable: false),
                    MaxRequestsPerSecondAuth = table.Column<int>(type: "int", nullable: false),
                    EnableRateLimiting = table.Column<bool>(type: "bit", nullable: false),
                    ExemptAdminsFromRateLimiting = table.Column<bool>(type: "bit", nullable: false),
                    MaxFailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    FailedLoginWindowMinutes = table.Column<int>(type: "int", nullable: false),
                    IpBlockDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    EnableIpBlocking = table.Column<bool>(type: "bit", nullable: false),
                    AccountLockoutMaxAttempts = table.Column<int>(type: "int", nullable: false),
                    AccountLockoutDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    EnableAccountLockout = table.Column<bool>(type: "bit", nullable: false),
                    EnableSecurityAuditLogging = table.Column<bool>(type: "bit", nullable: false),
                    AuditLogRetentionDays = table.Column<int>(type: "int", nullable: false),
                    NotifyOnCriticalEvents = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnIpBlocking = table.Column<bool>(type: "bit", nullable: false),
                    NotificationEmail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnableGeoIpBlocking = table.Column<bool>(type: "bit", nullable: false),
                    BlockedCountries = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WhitelistedIps = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BlacklistedIps = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuritySettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecuritySettings");
        }
    }
}
