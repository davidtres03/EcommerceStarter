using System.Security.Claims;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Checkout
{
    // Removed [Authorize] to allow guest checkout
    public class PaymentModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentModel> _logger;
        private readonly IPaymentService _paymentService;
        private readonly IStripeConfigService _stripeConfig;
        private readonly IOrderNumberService _orderNumberService;
        private readonly IEmailService _emailService;
        private readonly ISiteSettingsService _siteSettingsService;

        public PaymentModel(
            ICartService cartService,
            ApplicationDbContext context,
            ILogger<PaymentModel> logger,
            IPaymentService paymentService,
            IStripeConfigService stripeConfig,
            IOrderNumberService orderNumberService,
            IEmailService emailService,
            ISiteSettingsService siteSettingsService)
        {
            _cartService = cartService;
            _context = context;
            _logger = logger;
            _paymentService = paymentService;
            _stripeConfig = stripeConfig;
            _orderNumberService = orderNumberService;
            _emailService = emailService;
            _siteSettingsService = siteSettingsService;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal Subtotal { get; set; }
        
        public decimal Total { get; set; }
        public string StripePublishableKey { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        // Checkout information from session
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingZip { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(string? payment_intent, string? redirect_status)
        {
            // Load cart first to get accurate Subtotal
            LoadCart();

            // Handle redirect back from payment methods that require redirect (Cash App, etc.)
            if (!string.IsNullOrEmpty(payment_intent) && !string.IsNullOrEmpty(redirect_status))
            {
                _logger.LogInformation($"Payment redirect received: {payment_intent}, status: {redirect_status}");

                if (redirect_status == "succeeded")
                {
                    // Payment succeeded, verify it and create order
                    var paymentConfirmed = await _paymentService.ConfirmPaymentAsync(payment_intent);
                    
                    if (paymentConfirmed)
                    {
                        // Payment is confirmed, process the order
                        return await ProcessOrderAsync(payment_intent);
                    }
                    else
                    {
                        // Payment verification failed - stay on page with error
                        TempData["ErrorMessage"] = "Payment verification failed. Please try a different payment method or contact support.";
                        _logger.LogWarning($"Payment verification failed for intent {payment_intent}");
                    }
                }
                else if (redirect_status == "failed")
                {
                    // Payment failed - stay on page with specific error
                    TempData["ErrorMessage"] = "Your payment was declined. Please check your payment details and try again, or use a different payment method.";
                    _logger.LogWarning($"Payment failed for intent {payment_intent}");
                }
                else if (redirect_status == "canceled")
                {
                    // User canceled - stay on page with info message
                    TempData["InfoMessage"] = "Payment was canceled. You can try again when you're ready.";
                    _logger.LogInformation($"Payment canceled by user for intent {payment_intent}");
                }
                else if (redirect_status == "processing")
                {
                    TempData["InfoMessage"] = "Your payment is still processing. Please wait a moment and refresh the page.";
                    _logger.LogInformation($"Payment is processing for intent {payment_intent}");
                }
                else if (redirect_status == "requires_payment_method")
                {
                    TempData["ErrorMessage"] = "Your payment method was declined. Please try a different payment method.";
                    _logger.LogWarning($"Payment requires a new payment method for intent {payment_intent}");
               }
                else
                {
                    // Other status (processing, requires_payment_method, etc.)
                    TempData["ErrorMessage"] = $"Payment status: {redirect_status}. Please try again or contact support if this persists.";
                    _logger.LogWarning($"Unexpected payment status '{redirect_status}' for intent {payment_intent}");
                }
                
                // Remove the query parameters and reload the page to show the error
                return RedirectToPage();
            }

            if (!CartItems.Any())
            {
                _logger.LogWarning("Cart is empty on payment page - redirecting to cart");
                return RedirectToPage("/Cart/Index");
            }

            // Payment method selection is now handled by Stripe Payment Element
            // All available methods will be shown to the user

            // Validate that checkout information exists in session
            var fullName = HttpContext.Session.GetString("CheckoutFullName") ?? "";
            Email = HttpContext.Session.GetString("CheckoutEmail") ?? "";
            PhoneNumber = HttpContext.Session.GetString("CheckoutPhone") ?? "";
            ShippingAddress = HttpContext.Session.GetString("CheckoutAddress") ?? "";
            ShippingCity = HttpContext.Session.GetString("CheckoutCity") ?? "";
            ShippingState = HttpContext.Session.GetString("CheckoutState") ?? "";
            ShippingZip = HttpContext.Session.GetString("CheckoutZip") ?? "";
            

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(ShippingAddress))
            {
                // Redirect back to checkout if information is missing
                _logger.LogWarning("Missing checkout information - redirecting to checkout page");
                TempData["ErrorMessage"] = "Your session has expired. Please re-enter your shipping information to continue.";
                TempData["SessionExpired"] = "true";
                return RedirectToPage("/Checkout/Index");
            }

            // Parse tax amount
            
            
            // Recalculate total AFTER loading cart AND tax from session
            Total = Subtotal;
            
            _logger.LogInformation($"Payment page loaded - Items: {CartItems.Count}, Subtotal: {Subtotal:C}, Total: {Total:C}");

            // Validate Total is not zero
            if (Total <= 0)
            {
                _logger.LogError($"Invalid total amount: {Total:C} - cannot create payment intent");
                TempData["ErrorMessage"] = "Error calculating order total. Please go back and try again.";
                return RedirectToPage("/Checkout/Index");
            }

            // Get publishable key from secure storage
            StripePublishableKey = await _stripeConfig.GetPublishableKeyAsync();

            if (string.IsNullOrEmpty(StripePublishableKey))
            {
                _logger.LogError("Stripe publishable key not configured");
                TempData["ErrorMessage"] = "Payment system is not configured. Please contact support.";
                return RedirectToPage("/Checkout/Index");
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                _logger.LogInformation($"Creating payment intent - Amount: {Total:C}, User: {userId ?? "GUEST"}");

                var metadata = new Dictionary<string, string>
                {
                    { "cart_items", CartItems.Count.ToString() },
                    { "email", Email },
                    { "shipping_state", ShippingState },
                    { "order_source", "Website" }
                };

                var customerInfo = new CustomerInfo
                {
                    Email = Email,
                    Name = fullName,
                    Phone = PhoneNumber,
                    Address = new AddressInfo
                    {
                        Line1 = ShippingAddress,
                        City = ShippingCity,
                        State = ShippingState,
                        PostalCode = ShippingZip,
                        Country = "US"
                    }
                };

                // Include all supported payment methods, including CashApp
                ClientSecret = await _paymentService.CreatePaymentIntentAsync(
                    Total,
                    "usd",
                    metadata,
                    customerInfo
                );
                
                if (string.IsNullOrEmpty(ClientSecret))
                {
                    _logger.LogError("Payment Intent creation returned null ClientSecret");
                    throw new Exception("Payment Intent creation returned null ClientSecret");
                }
                
                _logger.LogInformation("Payment Intent created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent");
                
                ClientSecret = string.Empty;
                
                ModelState.AddModelError(string.Empty, $"Error initializing payment: {ex.Message}. Please try again or contact support.");
                TempData["ErrorMessage"] = $"Payment system error: {ex.Message}";
                return RedirectToPage("/Checkout/Index");
            }

            return Page();
        }

        private async Task<IActionResult> ProcessOrderAsync(string paymentIntentId)
        {
            LoadCart();

            if (!CartItems.Any())
            {
                return RedirectToPage("/Cart/Index");
            }

            // Retrieve checkout information from session
            var fullName = HttpContext.Session.GetString("CheckoutFullName") ?? "";
            var email = HttpContext.Session.GetString("CheckoutEmail") ?? "";
            var phoneNumber = HttpContext.Session.GetString("CheckoutPhone") ?? "";
            var shippingAddress = HttpContext.Session.GetString("CheckoutAddress") ?? "";
            var shippingCity = HttpContext.Session.GetString("CheckoutCity") ?? "";
            var shippingState = HttpContext.Session.GetString("CheckoutState") ?? "";
            var shippingZip = HttpContext.Session.GetString("CheckoutZip") ?? "";
            

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(shippingAddress))
            {
                return RedirectToPage("/Checkout/Index");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // For guest checkout, userId will be null
            var isGuestCheckout = HttpContext.Session.GetString("IsGuestCheckout") == "true";
            
            if (string.IsNullOrEmpty(userId) && !isGuestCheckout)
            {
                // Not logged in and not guest checkout - redirect to checkout
                return RedirectToPage("/Checkout/Index");
            }

            // Validate stock availability
            var productIds = CartItems.Select(c => c.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var stockErrors = new List<string>();
            foreach (var cartItem in CartItems)
            {
                var product = products.FirstOrDefault(p => p.Id == cartItem.ProductId);

                if (product == null)
                {
                    stockErrors.Add($"{cartItem.ProductName} is no longer available.");
                    continue;
                }

                if (!product.IsAvailable)
                {
                    stockErrors.Add($"{product.Name} is currently out of stock.");
                    continue;
                }

                if (product.StockQuantity < cartItem.Quantity)
                {
                    stockErrors.Add($"{product.Name}: Only {product.StockQuantity} available.");
                }
            }

            if (stockErrors.Any())
            {
                _logger.LogWarning($"Stock validation failed during payment: {string.Join(", ", stockErrors)}");
                TempData["ErrorMessage"] = "Some items are no longer available. Please update your cart.";
                return RedirectToPage("/Cart/Index");
            }

            // Start database transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Deduct stock
                foreach (var cartItem in CartItems)
                {
                    var product = products.First(p => p.Id == cartItem.ProductId);

                    if (product.StockQuantity < cartItem.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for {product.Name}");
                    }

                    product.StockQuantity -= cartItem.Quantity;

                    _logger.LogInformation(
                        $"Stock updated for product {product.Id} ({product.Name}): {product.StockQuantity + cartItem.Quantity} ? {product.StockQuantity}"
                    );
                }

                await _context.SaveChangesAsync();

                // Parse tax
                
                var total = Subtotal;

                // Create order
                // Generate unique order number
                var orderNumber = await _orderNumberService.GenerateUniqueOrderNumberAsync(
                    async (num) => await _context.Orders.AnyAsync(o => o.OrderNumber == num));

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,  // Null for guest orders
                    CustomerEmail = email,
                    ShippingName = fullName,
                    OrderDate = DateTime.UtcNow,
                    Subtotal = Subtotal,
                    TotalAmount = total,
                    Status = OrderStatus.Processing,
                    ShippingAddress = shippingAddress,
                    ShippingCity = shippingCity,
                    ShippingState = shippingState,
                    ShippingZip = shippingZip,
                    PaymentIntentId = paymentIntentId,
                    PaymentStatus = PaymentStatus.Succeeded
                };

                // Add order items
                foreach (var cartItem in CartItems)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Price
                    });
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Update user contact info if logged in (not guest)
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        bool userUpdated = false;
                        
                        if (string.IsNullOrEmpty(user.PhoneNumber) || user.PhoneNumber != phoneNumber)
                        {
                            user.PhoneNumber = phoneNumber;
                            userUpdated = true;
                        }
                        
                        if (userUpdated)
                        {
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Updated contact info for user {userId}");
                        }
                    }
                }

                // Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation($"Order {order.Id} ({order.OrderNumber}) created successfully for {(string.IsNullOrEmpty(userId) ? "guest" : "user " + userId)} with payment {paymentIntentId}");

                // Send emails based on site settings
                await SendOrderEmailsAsync(order);

                // Clear cart and session data
                _cartService.ClearCart();
                HttpContext.Session.Remove("CheckoutFullName");
                HttpContext.Session.Remove("CheckoutEmail");
                HttpContext.Session.Remove("CheckoutPhone");
                HttpContext.Session.Remove("CheckoutAddress");
                HttpContext.Session.Remove("CheckoutCity");
                HttpContext.Session.Remove("CheckoutState");
                HttpContext.Session.Remove("CheckoutZip");
                
                HttpContext.Session.Remove("CheckoutPaymentMethod");
                HttpContext.Session.Remove("IsGuestCheckout");

                // Redirect to confirmation
                return RedirectToPage("/Orders/Confirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error creating order");

                TempData["ErrorMessage"] = "An error occurred while processing your order. Please contact support.";
                return RedirectToPage("/Checkout/Payment");
            }
        }

        private async Task SendOrderEmailsAsync(Order order)
        {
            try
            {
                // Get site settings to check email notification preferences
                var settings = await _siteSettingsService.GetSettingsAsync();

                // Send order confirmation email to customer
                if (settings.EnableEmailNotifications && settings.SendOrderConfirmationEmails)
                {
                    try
                    {
                        var confirmationSent = await _emailService.SendOrderConfirmationAsync(order);
                        if (confirmationSent)
                        {
                            _logger.LogInformation($"Order confirmation email sent to {order.CustomerEmail} for order {order.OrderNumber}");
                        }
                        else
                        {
                            _logger.LogWarning($"Order confirmation email failed to send for order {order.OrderNumber} to {order.CustomerEmail}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending order confirmation email for order {order.OrderNumber}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Order confirmation email skipped for order {order.OrderNumber} - notifications disabled or setting not configured");
                }

                // Send admin notification email
                if (settings.EnableEmailNotifications && settings.SendAdminOrderNotifications && !string.IsNullOrEmpty(settings.AdminNotificationEmail))
                {
                    try
                    {
                        var adminNotificationSent = await _emailService.SendAdminOrderNotificationAsync(order);
                        if (adminNotificationSent)
                        {
                            _logger.LogInformation($"Admin order notification sent to {settings.AdminNotificationEmail} for order {order.OrderNumber}");
                        }
                        else
                        {
                            _logger.LogWarning($"Admin order notification failed to send for order {order.OrderNumber}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending admin order notification for order {order.OrderNumber}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Admin notification skipped for order {order.OrderNumber} - notifications disabled or setting not configured");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving site settings for email notifications");
            }
        }

        private void LoadCart()
        {
            CartItems = _cartService.GetCart();
            Subtotal = _cartService.GetCartTotal();
        }

        public IActionResult OnPost()
        {
            // This handler is called when Stripe confirmPayment redirects back with payment_intent
            // The actual payment processing happens via query parameters in OnGetAsync
            // This is just a safety fallback
            _logger.LogInformation("Payment form submitted - redirecting to payment processing");
            return RedirectToPage();
        }
    }
}



