using Stripe;

namespace EcommerceStarter.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly IStripeConfigService _stripeConfig;
        private readonly ILogger<StripePaymentService> _logger;
        private bool _apiKeySet = false;

        public StripePaymentService(
            IStripeConfigService stripeConfig,
            ILogger<StripePaymentService> logger)
        {
            _stripeConfig = stripeConfig;
            _logger = logger;
        }

        private async Task EnsureApiKeySetAsync()
        {
            if (!_apiKeySet)
            {
                StripeConfiguration.ApiKey = await _stripeConfig.GetSecretKeyAsync();
                _apiKeySet = true;
            }
        }

        public async Task<string> CreatePaymentIntentAsync(
            decimal amount, 
            string currency = "usd", 
            Dictionary<string, string>? metadata = null,
            CustomerInfo? customerInfo = null)
        {
            await EnsureApiKeySetAsync();

            try
            {
                string? customerId = null;

                // Create or retrieve Stripe customer if customer info provided
                if (customerInfo != null && !string.IsNullOrEmpty(customerInfo.Email))
                {
                    customerId = await GetOrCreateStripeCustomerAsync(customerInfo);
                }

                // Get requested payment method from metadata
                var requestedMethod = metadata?.GetValueOrDefault("payment_method_type") ?? "card";
                
                _logger.LogInformation($"Creating payment intent for method: {requestedMethod}, amount: {amount:C}");

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // Stripe expects amount in cents
                    Currency = currency,
                    Metadata = metadata ?? new Dictionary<string, string>(),
                    Customer = customerId,
                    ReceiptEmail = customerInfo?.Email
                };

                // Configure payment methods based on requested method
                // KEY PRINCIPLE: Let Stripe's Payment Element determine what's available
                // We tell Stripe which methods to support, and it auto-detects device/region availability
                
                switch (requestedMethod)
                {
                    case "card":
                    case "wallet":
                    case "cashapp":
                    case "link":
                    default:
                        // All methods use automatic payment methods
                        // Stripe Payment Element will show all available methods based on:
                        // - Device type
                        // - Browser
                        // - Region
                        // - User's saved payment methods
                        // 
                        // Available methods typically include:
                        // - Card (always)
                        // - Google Pay (Android/Chrome)
                        // - Apple Pay (iPhone/Safari)
                        // - Cash App Pay (US)
                        // - Link (if enabled)
                        options.AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true,
                            AllowRedirects = "always"
                        };
                        break;
                }

                // Add shipping information if provided (required for Apple Pay and Google Pay validation)
                if (customerInfo?.Address != null)
                {
                    options.Shipping = new ChargeShippingOptions
                    {
                        Name = customerInfo.Name ?? customerInfo.Email,
                        Phone = customerInfo.Phone,
                        Address = new AddressOptions
                        {
                            Line1 = customerInfo.Address.Line1,
                            City = customerInfo.Address.City,
                            State = customerInfo.Address.State,
                            PostalCode = customerInfo.Address.PostalCode,
                            Country = customerInfo.Address.Country ?? "US"
                        }
                    };
                }

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                _logger.LogInformation(
                    $"Payment Intent created: {paymentIntent.Id} for {amount:C} " +
                    $"with requested method: {requestedMethod} | " +
                    $"Available payment methods: {string.Join(", ", paymentIntent.PaymentMethodTypes)}" +
                    (customerId != null ? $" | customer: {customerId}" : "")
                );

                return paymentIntent.ClientSecret;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                throw;
            }
        }

        private async Task<string> GetOrCreateStripeCustomerAsync(CustomerInfo customerInfo)
        {
            try
            {
                var customerService = new CustomerService();

                // Try to find existing customer by email
                var searchOptions = new CustomerSearchOptions
                {
                    Query = $"email:'{customerInfo.Email}'",
                    Limit = 1
                };

                var existingCustomers = await customerService.SearchAsync(searchOptions);

                if (existingCustomers.Data.Any())
                {
                    var existingCustomer = existingCustomers.Data.First();
                    _logger.LogInformation($"Found existing Stripe customer: {existingCustomer.Id} for email {customerInfo.Email}");
                    
                    // Update customer info if needed
                    await UpdateStripeCustomerAsync(existingCustomer.Id, customerInfo);
                    
                    return existingCustomer.Id;
                }

                // Create new customer if not found
                var createOptions = new CustomerCreateOptions
                {
                    Email = customerInfo.Email,
                    Name = customerInfo.Name,
                    Phone = customerInfo.Phone,
                    Metadata = new Dictionary<string, string>
                    {
                        { "source", "EcommerceStarter" }
                    }
                };

                // Add address if provided
                if (customerInfo.Address != null)
                {
                    createOptions.Address = new AddressOptions
                    {
                        Line1 = customerInfo.Address.Line1,
                        City = customerInfo.Address.City,
                        State = customerInfo.Address.State,
                        PostalCode = customerInfo.Address.PostalCode,
                        Country = customerInfo.Address.Country ?? "US"
                    };
                }

                var customer = await customerService.CreateAsync(createOptions);
                
                _logger.LogInformation($"Created new Stripe customer: {customer.Id} for email {customerInfo.Email}");

                return customer.Id;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error creating/retrieving Stripe customer for {customerInfo.Email}");
                // Return null to continue without customer association
                return null!;
            }
        }

        private async Task UpdateStripeCustomerAsync(string customerId, CustomerInfo customerInfo)
        {
            try
            {
                var customerService = new CustomerService();
                
                var updateOptions = new CustomerUpdateOptions
                {
                    Name = customerInfo.Name,
                    Phone = customerInfo.Phone
                };

                if (customerInfo.Address != null)
                {
                    updateOptions.Address = new AddressOptions
                    {
                        Line1 = customerInfo.Address.Line1,
                        City = customerInfo.Address.City,
                        State = customerInfo.Address.State,
                        PostalCode = customerInfo.Address.PostalCode,
                        Country = customerInfo.Address.Country ?? "US"
                    };
                }

                await customerService.UpdateAsync(customerId, updateOptions);
                
                _logger.LogInformation($"Updated Stripe customer: {customerId}");
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, $"Error updating Stripe customer {customerId}");
                // Don't throw - updating customer info is not critical
            }
        }

        public async Task<bool> ConfirmPaymentAsync(string paymentIntentId)
        {
            await EnsureApiKeySetAsync();

            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);

                return paymentIntent.Status == "succeeded";
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error confirming payment intent {paymentIntentId}");
                return false;
            }
        }

        public async Task<string> GetPaymentStatusAsync(string paymentIntentId)
        {
            await EnsureApiKeySetAsync();

            try
            {
                var service = new PaymentIntentService();
                var paymentIntent = await service.GetAsync(paymentIntentId);

                return paymentIntent.Status;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, $"Error getting payment status for {paymentIntentId}");
                return "error";
            }
        }

        public async Task<(bool success, string? refundId, string? error)> RefundPaymentAsync(
            string paymentIntentId, 
            long amountInCents, 
            string reason = "requested_by_customer")
        {
            await EnsureApiKeySetAsync();

            try
            {
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Amount = amountInCents,
                    Reason = reason
                };

                var service = new RefundService();
                var refund = await service.CreateAsync(refundOptions);

                _logger.LogInformation(
                    "Refund created: {RefundId} for PaymentIntent {PaymentIntentId}, Amount: ${Amount}, Status: {Status}",
                    refund.Id, paymentIntentId, amountInCents / 100.0, refund.Status
                );

                return (true, refund.Id, null);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error refunding payment intent {PaymentIntentId}", paymentIntentId);
                return (false, null, ex.Message);
            }
        }
    }
}
