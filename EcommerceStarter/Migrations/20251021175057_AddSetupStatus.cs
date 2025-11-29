using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddSetupStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SetupStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsSetupComplete = table.Column<bool>(type: "bit", nullable: false),
                    SetupCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SetupCompletedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HasConfiguredBranding = table.Column<bool>(type: "bit", nullable: false),
                    HasConfiguredStripe = table.Column<bool>(type: "bit", nullable: false),
                    HasAddedProducts = table.Column<bool>(type: "bit", nullable: false),
                    HasConfiguredSecurity = table.Column<bool>(type: "bit", nullable: false),
                    PlatformVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InitialTheme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SetupNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetupStatus", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SetupStatus");
        }
    }
}
