using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceErrorLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceStatusLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsWebServiceOnline = table.Column<bool>(type: "bit", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: false),
                    IsBackgroundServiceRunning = table.Column<bool>(type: "bit", nullable: false),
                    PendingOrdersCount = table.Column<int>(type: "int", nullable: false),
                    MemoryUsageMb = table.Column<int>(type: "int", nullable: false),
                    CpuUsagePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DatabaseConnected = table.Column<bool>(type: "bit", nullable: false),
                    ActiveUserCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UptimePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStatusLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpdateHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplyDurationSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateHistories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceErrorLogs");

            migrationBuilder.DropTable(
                name: "ServiceStatusLogs");

            migrationBuilder.DropTable(
                name: "UpdateHistories");
        }
    }
}
