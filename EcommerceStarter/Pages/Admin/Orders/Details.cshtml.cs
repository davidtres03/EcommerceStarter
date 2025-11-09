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

        public DetailsModel(
            ApplicationDbContext context, 
            ILogger<DetailsModel> logger,
            IEmailService emailService,
            IAuditLogService auditLogService,
            ICourierService courierService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _auditLogService = auditLogService;
            _courierService = courierService;
        }

        public Order? Order { get; set; }
        public IEnumerable<Courier> AvailableCouriers { get; set; } = new List<Courier>();

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
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
    }
}
