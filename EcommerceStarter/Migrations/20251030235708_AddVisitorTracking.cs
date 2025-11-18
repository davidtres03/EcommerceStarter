using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitorSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Referrer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LandingPage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PageViewCount = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Converted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PageTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Referrer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeOnPage = table.Column<int>(type: "int", nullable: true),
                    QueryString = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageViews_VisitorSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VisitorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitorEvents_VisitorSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "VisitorSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageViews_SessionId",
                table: "PageViews",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PageViews_Timestamp",
                table: "PageViews",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PageViews_Url",
                table: "PageViews",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorEvents_Action",
                table: "VisitorEvents",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorEvents_Category",
                table: "VisitorEvents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorEvents_SessionId",
                table: "VisitorEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorEvents_Timestamp",
                table: "VisitorEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorSessions_IpAddress",
                table: "VisitorSessions",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorSessions_SessionId",
                table: "VisitorSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorSessions_StartTime",
                table: "VisitorSessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorSessions_UserId",
                table: "VisitorSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageViews");

            migrationBuilder.DropTable(
                name: "VisitorEvents");

            migrationBuilder.DropTable(
                name: "VisitorSessions");
        }
    }
}
