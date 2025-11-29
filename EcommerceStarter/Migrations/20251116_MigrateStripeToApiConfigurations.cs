using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceStarter.Migrations
{
    /// <inheritdoc />
    public partial class MigrateStripeToApiConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Copy Stripe configurations from StripeConfigurations to ApiConfigurations
            // We'll insert each Stripe config as a row with ApiType="Stripe"
            migrationBuilder.Sql(@"
                INSERT INTO ApiConfigurations 
                (ApiType, Name, IsActive, IsTestMode, EncryptedValue1, EncryptedValue2, EncryptedValue3, 
                 Description, CreatedAt, LastUpdated, CreatedBy, UpdatedBy)
                SELECT 
                    'Stripe' AS ApiType,
                    CASE WHEN IsTestMode = 1 THEN 'Stripe-Test' ELSE 'Stripe-Live' END AS Name,
                    1 AS IsActive,
                    IsTestMode,
                    EncryptedPublishableKey AS EncryptedValue1,
                    EncryptedSecretKey AS EncryptedValue2,
                    EncryptedWebhookSecret AS EncryptedValue3,
                    CASE WHEN IsTestMode = 1 THEN 'Stripe Test Mode Configuration' ELSE 'Stripe Live Mode Configuration' END AS Description,
                    GETUTCDATE() AS CreatedAt,
                    LastUpdated,
                    UpdatedBy AS CreatedBy,
                    UpdatedBy
                FROM StripeConfigurations
                WHERE EncryptedPublishableKey IS NOT NULL 
                   OR EncryptedSecretKey IS NOT NULL 
                   OR EncryptedWebhookSecret IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete Stripe configs from ApiConfigurations
            migrationBuilder.Sql("DELETE FROM ApiConfigurations WHERE ApiType = 'Stripe';");
        }
    }
}
