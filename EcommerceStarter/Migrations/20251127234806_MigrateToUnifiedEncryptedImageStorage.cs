using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToUnifiedEncryptedImageStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "StoredImages");

            migrationBuilder.DropColumn(
                name: "EmailLogoBase64",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FaviconBase64",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroImageBase64",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HorizontalLogoBase64",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LogoBase64",
                table: "SiteSettings");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedData",
                table: "StoredImages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StorageType",
                table: "StoredImages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "EmailLogoImageId",
                table: "SiteSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FaviconImageId",
                table: "SiteSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HeroImageId",
                table: "SiteSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HorizontalLogoImageId",
                table: "SiteSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LogoImageId",
                table: "SiteSettings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_EmailLogoImageId",
                table: "SiteSettings",
                column: "EmailLogoImageId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_FaviconImageId",
                table: "SiteSettings",
                column: "FaviconImageId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_HeroImageId",
                table: "SiteSettings",
                column: "HeroImageId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_HorizontalLogoImageId",
                table: "SiteSettings",
                column: "HorizontalLogoImageId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_LogoImageId",
                table: "SiteSettings",
                column: "LogoImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_StoredImages_EmailLogoImageId",
                table: "SiteSettings",
                column: "EmailLogoImageId",
                principalTable: "StoredImages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_StoredImages_FaviconImageId",
                table: "SiteSettings",
                column: "FaviconImageId",
                principalTable: "StoredImages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_StoredImages_HeroImageId",
                table: "SiteSettings",
                column: "HeroImageId",
                principalTable: "StoredImages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_StoredImages_HorizontalLogoImageId",
                table: "SiteSettings",
                column: "HorizontalLogoImageId",
                principalTable: "StoredImages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_StoredImages_LogoImageId",
                table: "SiteSettings",
                column: "LogoImageId",
                principalTable: "StoredImages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_StoredImages_EmailLogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_StoredImages_FaviconImageId",
                table: "SiteSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_StoredImages_HeroImageId",
                table: "SiteSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_StoredImages_HorizontalLogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_StoredImages_LogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_EmailLogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_FaviconImageId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_HeroImageId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_HorizontalLogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_LogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EncryptedData",
                table: "StoredImages");

            migrationBuilder.DropColumn(
                name: "StorageType",
                table: "StoredImages");

            migrationBuilder.DropColumn(
                name: "EmailLogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FaviconImageId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeroImageId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HorizontalLogoImageId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LogoImageId",
                table: "SiteSettings");

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "StoredImages",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "EmailLogoBase64",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaviconBase64",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroImageBase64",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HorizontalLogoBase64",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoBase64",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
