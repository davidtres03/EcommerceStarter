using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/orders")]
    public class OrdersApiController : ControllerBase
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersApiController> _logger;

        public OrdersApiController(ApplicationDbContext context, ILogger<OrdersApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.User)
                    .AsQueryable();

                // Apply status filter
                if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    query = query.Where(o => o.Status == orderStatus);
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(o =>
                        o.OrderNumber.Contains(search) ||
                        o.CustomerEmail.Contains(search) ||
                        o.ShippingName.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        CustomerId = o.UserId != null ? int.Parse(o.UserId) : 0,
                        CustomerName = o.ShippingName,
                        o.CustomerEmail,
                        Status = o.Status.ToString(),
                        TotalAmount = o.TotalAmount,
                        ItemCount = o.OrderItems.Count,
                        ShippingAddress = $"{o.ShippingAddress}, {o.ShippingCity}, {o.ShippingState} {o.ShippingZip}",
                        CreatedAt = o.OrderDate.ToString(DateTimeFormat),
                        UpdatedAt = (string?)null,
                        DeliveredAt = o.Status == OrderStatus.Delivered ? o.OrderDate.ToString(DateTimeFormat) : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    orders,
                    totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders");
                return StatusCode(500, new { message = "Error fetching orders" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { message = "Order not found" });

                return Ok(new
                {
                    order.Id,
                    order.OrderNumber,
                    CustomerId = order.UserId != null ? int.Parse(order.UserId) : 0,
                    CustomerName = order.ShippingName,
                    order.CustomerEmail,
                    Status = order.Status.ToString(),
                    TotalAmount = order.TotalAmount,
                    ItemCount = order.OrderItems.Count,
                    ShippingAddress = $"{order.ShippingAddress}, {order.ShippingCity}, {order.ShippingState} {order.ShippingZip}",
                    CreatedAt = order.OrderDate.ToString(DateTimeFormat),
                    UpdatedAt = (string?)null,
                    DeliveredAt = order.Status == OrderStatus.Delivered ? order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss") : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order {OrderId}", id);
                return StatusCode(500, new { message = "Error fetching order" });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromQuery] string status)
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                    return BadRequest(new { message = "Invalid status value" });

                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { message = "Order not found" });

                order.Status = orderStatus;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order {OrderId} status", id);
                return StatusCode(500, new { message = "Error updating order status" });
            }
        }
    }
}
