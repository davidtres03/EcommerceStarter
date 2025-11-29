using Microsoft.AspNetCore.Mvc;
using Stripe;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            IConfiguration configuration,
            ApplicationDbContext context,
            ILogger<StripeWebhookController> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"] // Add this to appsettings
                );

                _logger.LogInformation($"Stripe webhook received: {stripeEvent.Type}");

                // Handle the event
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandlePaymentIntentSucceeded(paymentIntent);
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandlePaymentIntentFailed(paymentIntent);
                }
                else if (stripeEvent.Type == "charge.refunded")
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    await HandleChargeRefunded(charge);
                }
                else
                {
                    _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook error");
                return BadRequest();
            }
        }

        private async Task HandlePaymentIntentSucceeded(PaymentIntent? paymentIntent)
        {
            if (paymentIntent == null) return;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntent.Id);

            if (order != null)
            {
                order.PaymentStatus = PaymentStatus.Succeeded;
                order.Status = OrderStatus.Processing;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Order {order.Id} payment confirmed via webhook");
            }
        }

        private async Task HandlePaymentIntentFailed(PaymentIntent? paymentIntent)
        {
            if (paymentIntent == null) return;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntent.Id);

            if (order != null)
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();
                
                _logger.LogWarning($"Order {order.Id} payment failed via webhook");
            }
        }

        private async Task HandleChargeRefunded(Charge? charge)
        {
            if (charge == null) return;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PaymentIntentId == charge.PaymentIntentId);

            if (order != null)
            {
                order.PaymentStatus = PaymentStatus.Refunded;
                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Order {order.Id} refunded via webhook");
            }
        }
    }
}
