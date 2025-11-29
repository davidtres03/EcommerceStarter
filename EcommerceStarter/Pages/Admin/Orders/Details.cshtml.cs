using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin.Orders
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;
        private readonly ICourierService _courierService;
        private readonly IPaymentService _paymentService;

        public DetailsModel(
            ApplicationDbContext context, 
            ILogger<DetailsModel> logger,
            IEmailService emailService,
            IAuditLogService auditLogService,
            ICourierService courierService,
            IPaymentService paymentService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _auditLogService = auditLogService;
            _courierService = courierService;
            _paymentService = paymentService;
        }

        public Order? Order { get; set; }
        public IEnumerable<Courier> AvailableCouriers { get; set; } = new List<Courier>();

        [TempData]
        public string? SuccessMessage { get; set; }
        
        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.RefundHistories)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (Order == null)
            {
                return Page();
            }

            AvailableCouriers = _courierService.GetAvailableCouriers();

            return Page();
        }

        /// <summary>
        /// AJAX handler to detect courier from tracking number
        /// Called from JavaScript when user enters/changes tracking number
        /// </summary>
        public JsonResult OnGetDetectCourierAsync(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                return new JsonResult(new { courier = 0, detected = false });
            }

            var detectedCourier = _courierService.DetectCourierFromTrackingNumber(trackingNumber);

            return new JsonResult(new 
            { 
                courier = (int)detectedCourier,
                detected = detectedCourier != Courier.Unknown,
                courierName = detectedCourier.GetShortName()
            });
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, int status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return RedirectToPage("./Index");
            }

            var oldStatus = order.Status;
            var newStatus = (OrderStatus)status;

            // If changing to Cancelled, restore stock
            if (newStatus == OrderStatus.Cancelled && oldStatus != OrderStatus.Cancelled)
            {
                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.Product != null)
                    {
                        orderItem.Product.StockQuantity += orderItem.Quantity;
                        
                        _logger.LogInformation(
                            $"Stock restored for product {orderItem.ProductId} ({orderItem.Product.Name}): " +
                            $"{orderItem.Product.StockQuantity - orderItem.Quantity} ? {orderItem.Product.StockQuantity} " +
                            $"due to order #{order.Id} cancellation"
                        );
                    }
                }
            }
            // If changing FROM Cancelled to another status, deduct stock again
            else if (oldStatus == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
            {
                // Validate stock is available
                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.Product != null)
                    {
                        if (orderItem.Product.StockQuantity < orderItem.Quantity)
                        {
                            SuccessMessage = $"Cannot reactivate order: Insufficient stock for {orderItem.Product.Name} " +
                                           $"(need {orderItem.Quantity}, have {orderItem.Product.StockQuantity})";
                            return RedirectToPage(new { id });
                        }
                    }
                }

                // Deduct stock
                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.Product != null)
                    {
                        orderItem.Product.StockQuantity -= orderItem.Quantity;
                        
                        _logger.LogInformation(
                            $"Stock deducted for product {orderItem.ProductId} ({orderItem.Product.Name}): " +
                            $"{orderItem.Product.StockQuantity + orderItem.Quantity} ? {orderItem.Product.StockQuantity} " +
                            $"due to order #{order.Id} reactivation"
                        );
                    }
                }
            }

            order.Status = newStatus;

            try
            {
                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    $"Order #{order.Id} status updated from {oldStatus} to {newStatus} by {User.Identity?.Name}"
                );

                SuccessMessage = $"Order status updated to {newStatus}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order #{order.Id} status");
                SuccessMessage = "Failed to update order status.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUpdatePaymentStatusAsync(int id, int paymentStatus)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return RedirectToPage("./Index");
            }

            var oldPaymentStatus = order.PaymentStatus;
            var newPaymentStatus = (PaymentStatus)paymentStatus;

            order.PaymentStatus = newPaymentStatus;

            try
            {
                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    $"Order #{order.Id} payment status updated from {oldPaymentStatus} to {newPaymentStatus} by {User.Identity?.Name}"
                );

                SuccessMessage = $"Payment status updated to {newPaymentStatus}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order #{order.Id} payment status");
                SuccessMessage = "Failed to update payment status.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAddTrackingNumberAsync(int id, string trackingNumber, int trackingCourier = 0)
        {
            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                TempData["Error"] = "Please enter a valid tracking number.";
                return RedirectToPage(new { id });
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return RedirectToPage("./Index");
            }

            var cleanedTrackingNumber = trackingNumber.Trim();
            
            // Detect courier if not explicitly selected (trackingCourier == 0 means auto-detect)
            var courier = trackingCourier == 0 
                ? _courierService.DetectCourierFromTrackingNumber(cleanedTrackingNumber)
                : (Courier)trackingCourier;

            // Store both tracking number and courier
            order.TrackingNumber = cleanedTrackingNumber;
            order.TrackingCourier = courier;

            try
            {
                await _context.SaveChangesAsync();

                // Send shipping notification email with tracking link
                var emailSent = await _emailService.SendShippingNotificationAsync(order, cleanedTrackingNumber);

                if (emailSent)
                {
                    _logger.LogInformation(
                        $"Tracking number added to order #{order.Id}: {cleanedTrackingNumber} (Courier: {courier.GetShortName()}) " +
                        $"by {User.Identity?.Name}. Shipping notification email sent to {order.CustomerEmail}"
                    );

                    // Log to customer audit (only if registered user)
                    if (!string.IsNullOrEmpty(order.UserId))
                    {
                        await _auditLogService.LogEmailSentAsync(
                            order.UserId,
                            "ShippingNotification",
                            $"Order #{order.Id} Shipping Update - Tracking: {cleanedTrackingNumber} ({courier.GetShortName()})",
                            success: true
                        );
                    }

                    SuccessMessage = $"Tracking number added ({courier.GetShortName()}) and customer notified via email with tracking link.";
                }
                else
                {
                    _logger.LogWarning(
                        $"Tracking number added to order #{order.Id}, but email notification failed"
                    );

                    // Log failed email to customer audit
                    if (!string.IsNullOrEmpty(order.UserId))
                    {
                        await _auditLogService.LogEmailSentAsync(
                            order.UserId,
                            "ShippingNotification",
                            $"Order #{order.Id} Shipping Update - Tracking: {cleanedTrackingNumber}",
                            success: false,
                            errorMessage: "Email service failed to send notification"
                        );
                    }

                    SuccessMessage = $"Tracking number added, but customer email notification failed.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding tracking number to order #{order.Id}");
                TempData["Error"] = "Failed to add tracking number.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostProcessRefundAsync(
            int id, 
            string refundType, 
            decimal? refundAmount, 
            string refundReason,
            bool restockInventory,
            string? refundNotes)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return RedirectToPage("./Index");
            }

            // Validate payment intent exists
            if (string.IsNullOrEmpty(order.PaymentIntentId))
            {
                ErrorMessage = "Cannot process refund: No payment information found for this order.";
                return RedirectToPage(new { id });
            }

            // Calculate refund amount
            decimal amountToRefund;
            if (refundType == "full")
            {
                amountToRefund = order.TotalAmount;
            }
            else if (refundAmount.HasValue && refundAmount.Value > 0)
            {
                amountToRefund = refundAmount.Value;
            }
            else
            {
                ErrorMessage = "Please enter a valid refund amount.";
                return RedirectToPage(new { id });
            }

            // Validate refund amount
            decimal alreadyRefunded = order.RefundedAmount ?? 0;
            decimal remainingAmount = order.TotalAmount - alreadyRefunded;

            if (amountToRefund > remainingAmount)
            {
                ErrorMessage = $"Refund amount (${amountToRefund:N2}) exceeds remaining order amount (${remainingAmount:N2}).";
                return RedirectToPage(new { id });
            }

            try
            {
                // Process Stripe refund
                long amountInCents = (long)(amountToRefund * 100);
                var (success, refundId, error) = await _paymentService.RefundPaymentAsync(
                    order.PaymentIntentId, 
                    amountInCents,
                    "requested_by_customer"
                );

                if (!success)
                {
                    ErrorMessage = $"Stripe refund failed: {error}";
                    _logger.LogError("Stripe refund failed for order {OrderId}: {Error}", order.Id, error);
                    return RedirectToPage(new { id });
                }

                // Update order payment status
                order.RefundedAmount = alreadyRefunded + amountToRefund;
                order.PaymentStatus = PaymentStatus.Refunded;
                order.RefundedDate = DateTime.UtcNow;

                // Create refund history entry
                var refundHistory = new RefundHistory
                {
                    OrderId = order.Id,
                    StripeRefundId = refundId ?? string.Empty,
                    RefundAmount = amountToRefund,
                    RefundType = refundType,
                    RefundReason = refundReason,
                    RefundNotes = refundNotes,
                    InventoryRestocked = restockInventory,
                    ProcessedBy = User.Identity?.Name ?? "Unknown",
                    ProcessedDate = DateTime.UtcNow,
                    RefundStatus = "succeeded"
                };
                _context.RefundHistories.Add(refundHistory);

                // Restock inventory if selected
                if (restockInventory)
                {
                    foreach (var orderItem in order.OrderItems)
                    {
                        if (orderItem.Product != null)
                        {
                            orderItem.Product.StockQuantity += orderItem.Quantity;
                            
                            _logger.LogInformation(
                                "Stock restocked for product {ProductId} ({ProductName}): {NewQuantity} due to refund on order #{OrderId}",
                                orderItem.ProductId,
                                orderItem.Product.Name,
                                orderItem.Product.StockQuantity,
                                order.Id
                            );
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Send refund email notification
                try
                {
                    await SendRefundEmailAsync(order, amountToRefund, refundReason);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send refund email for order {OrderId}", order.Id);
                    // Don't fail the refund if email fails
                }

                _logger.LogInformation(
                    "Refund processed for order #{OrderId}: ${Amount} ({Type}) by {ProcessedBy}. Stripe Refund ID: {RefundId}",
                    order.Id,
                    amountToRefund,
                    refundType,
                    User.Identity?.Name,
                    refundId
                );

                SuccessMessage = $"Refund of ${amountToRefund:N2} processed successfully. {(restockInventory ? "Inventory has been restocked." : "")}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for order {OrderId}", order.Id);
                ErrorMessage = "An error occurred while processing the refund. Please try again.";
            }

            return RedirectToPage(new { id });
        }

        private async Task SendRefundEmailAsync(Order order, decimal refundAmount, string refundReason)
        {
            // Basic refund email - could be enhanced with a dedicated email template
            var subject = $"Refund Processed for Order #{order.OrderNumber}";
            var body = $@"
                <h2>Refund Confirmation</h2>
                <p>Dear Customer,</p>
                <p>A refund has been processed for your order <strong>#{order.OrderNumber}</strong>.</p>
                <p><strong>Refund Amount:</strong> ${refundAmount:N2}</p>
                <p><strong>Reason:</strong> {GetRefundReasonDisplay(refundReason)}</p>
                <p>The refund will be credited to your original payment method within 5-10 business days.</p>
                <p>If you have any questions, please contact our support team.</p>
                <p>Thank you for your business.</p>
            ";

            // Use the email service to send the notification
            // Assuming IEmailService has a basic SendEmailAsync method
            // This would need to be implemented if not already available
        }

        private static string GetRefundReasonDisplay(string refundReason)
        {
            return refundReason switch
            {
                "defective" => "Defective Product",
                "wrong_item" => "Wrong Item Received",
                "customer_request" => "Customer Request",
                "other" => "Other",
                _ => refundReason
            };
        }
    }
}
