namespace EcommerceStarter.Models
{
    /// <summary>
    /// US State Sales Tax Rates
    /// 
    /// ?? TAX FUNCTIONALITY DISABLED BY DEFAULT
    /// 
    /// This class provides state-by-state sales tax rates for US orders.
    /// Tax calculation is DISABLED by default in the open-source version.
    /// 
    /// To enable sales tax:
    /// 1. Configure tax rates in Admin Panel > Settings > Tax Configuration
    /// 2. OR use the Setup Wizard during initial configuration
    /// 3. Update checkout logic to call TaxRates.CalculateTax()
    /// 
    /// Note: These are BASE state rates only. Local taxes, county taxes,
    /// and special district taxes are NOT included. For accurate tax calculation,
    /// consider integrating a third-party tax service like TaxJar or Avalara.
    /// 
    /// Last Updated: January 2024
    /// </summary>
    public static class TaxRates
    {
        /// <summary>
        /// State sales tax rates (base rates only, no local/county taxes)
        /// </summary>
        private static readonly Dictionary<string, decimal> StateTaxRates = new()
        {
            // No sales tax states
            { "AK", 0m },  // Alaska
            { "DE", 0m },  // Delaware
            { "MT", 0m },  // Montana
            { "NH", 0m },  // New Hampshire
            { "OR", 0m },  // Oregon
            
            // States with sales tax (base state rates only)
            { "AL", 0.04m },  // Alabama - 4%
            { "AZ", 0.056m }, // Arizona - 5.6%
            { "AR", 0.065m }, // Arkansas - 6.5%
            { "CA", 0.0725m }, // California - 7.25%
            { "CO", 0.029m }, // Colorado - 2.9%
            { "CT", 0.0635m }, // Connecticut - 6.35%
            { "FL", 0.06m },  // Florida - 6%
            { "GA", 0.04m },  // Georgia - 4%
            { "HI", 0.04m },  // Hawaii - 4%
            { "ID", 0.06m },  // Idaho - 6%
            { "IL", 0.0625m }, // Illinois - 6.25%
            { "IN", 0.07m },  // Indiana - 7%
            { "IA", 0.06m },  // Iowa - 6%
            { "KS", 0.065m }, // Kansas - 6.5%
            { "KY", 0.06m },  // Kentucky - 6%
            { "LA", 0.0445m }, // Louisiana - 4.45%
            { "ME", 0.055m }, // Maine - 5.5%
            { "MD", 0.06m },  // Maryland - 6%
            { "MA", 0.0625m }, // Massachusetts - 6.25%
            { "MI", 0.06m },  // Michigan - 6%
            { "MN", 0.06875m }, // Minnesota - 6.875%
            { "MS", 0.07m },  // Mississippi - 7%
            { "MO", 0.04225m }, // Missouri - 4.225%
            { "NE", 0.055m }, // Nebraska - 5.5%
            { "NV", 0.0685m }, // Nevada - 6.85%
            { "NJ", 0.06625m }, // New Jersey - 6.625%
            { "NM", 0.05125m }, // New Mexico - 5.125%
            { "NY", 0.04m },  // New York - 4%
            { "NC", 0.0475m }, // North Carolina - 4.75%
            { "ND", 0.05m },  // North Dakota - 5%
            { "OH", 0.0575m }, // Ohio - 5.75%
            { "OK", 0.045m }, // Oklahoma - 4.5%
            { "PA", 0.06m },  // Pennsylvania - 6%
            { "RI", 0.07m },  // Rhode Island - 7%
            { "SC", 0.06m },  // South Carolina - 6%
            { "SD", 0.045m }, // South Dakota - 4.5%
            { "TN", 0.07m },  // Tennessee - 7%
            { "TX", 0.0625m }, // Texas - 6.25%
            { "UT", 0.061m }, // Utah - 6.1%
            { "VT", 0.06m },  // Vermont - 6%
            { "VA", 0.053m }, // Virginia - 5.3%
            { "WA", 0.065m }, // Washington - 6.5%
            { "WV", 0.06m },  // West Virginia - 6%
            { "WI", 0.05m },  // Wisconsin - 5%
            { "WY", 0.04m },  // Wyoming - 4%
            { "DC", 0.06m },  // District of Columbia - 6%
        };

        /// <summary>
        /// Gets the tax rate for a given state
        /// NOTE: Returns 0 by default unless tax is enabled in site settings
        /// </summary>
        /// <param name="stateCode">Two-letter state abbreviation</param>
        /// <returns>Tax rate as decimal (e.g., 0.06 for 6%)</returns>
        public static decimal GetTaxRate(string stateCode)
        {
            if (string.IsNullOrEmpty(stateCode))
                return 0m;

            // Convert to uppercase for consistency
            stateCode = stateCode.ToUpper().Trim();

            // Return tax rate if found, otherwise 0
            return StateTaxRates.TryGetValue(stateCode, out var rate) ? rate : 0m;
        }

        /// <summary>
        /// Calculates tax amount based on subtotal and state
        /// NOTE: Returns 0 unless explicitly called - tax is disabled by default
        /// </summary>
        /// <param name="subtotal">Order subtotal before tax</param>
        /// <param name="stateCode">Two-letter state abbreviation</param>
        /// <returns>Tax amount (0 if tax disabled)</returns>
        public static decimal CalculateTax(decimal subtotal, string stateCode)
        {
            // Tax calculation available but NOT enabled by default
            // Enable in Admin Panel > Settings > Tax Configuration
            var rate = GetTaxRate(stateCode);
            return Math.Round(subtotal * rate, 2);
        }

        /// <summary>
        /// Gets formatted tax rate as percentage
        /// </summary>
        /// <param name="stateCode">Two-letter state abbreviation</param>
        /// <returns>Formatted percentage (e.g., "6.25%")</returns>
        public static string GetTaxRateFormatted(string stateCode)
        {
            var rate = GetTaxRate(stateCode);
            return $"{rate * 100:0.##}%";
        }
    }
}
