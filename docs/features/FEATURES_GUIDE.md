# ??? Features Implementation Guide
## MyStore Supply Co.

Complete guide for shopping cart, products, orders, and sales tax implementation.

---

## ?? Table of Contents

1. [Shopping Cart](#shopping-cart)
2. [Product Management](#product-management)
3. [Order System](#order-system)
4. [Sales Tax](#sales-tax)
5. [Customer Order History](#customer-order-history)

---

## ?? Shopping Cart

### Overview

Session-based shopping cart that persists across page loads and provides a smooth shopping experience.

### Features

- ? Session-based storage
- ? Add/Remove items
- ? Update quantities
- ? Real-time total calculation
- ? Persist across sessions
- ? Responsive design
- ? Toast notifications

### Cart Service

**ICartService Interface:**
```csharp
public interface ICartService
{
    Task<List<CartItem>> GetCartAsync();
    Task AddToCartAsync(int productId, int quantity = 1);
    Task UpdateQuantityAsync(int productId, int quantity);
    Task RemoveFromCartAsync(int productId);
    Task ClearCartAsync();
    Task<int> GetCartCountAsync();
    Task<decimal> GetCartTotalAsync();
}
```

**CartService Implementation:**
```csharp
public class CartService : ICartService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    private const string CartSessionKey = "ShoppingCart";
    
    public CartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }
    
    public async Task<List<CartItem>> GetCartAsync()
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var cartJson = session.GetString(CartSessionKey);
        
        if (string.IsNullOrEmpty(cartJson))
        {
            return new List<CartItem>();
        }
        
        var cartItems = JsonSerializer.Deserialize<List<CartItem>>(cartJson);
        
        // Load product details
        foreach (var item in cartItems)
        {
            item.Product = await _context.Products.FindAsync(item.ProductId);
        }
        
        return cartItems;
    }
    
    public async Task AddToCartAsync(int productId, int quantity = 1)
    {
        var cart = await GetCartAsync();
        var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);
        
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                throw new ArgumentException("Product not found");
            }
            
            cart.Add(new CartItem
            {
                ProductId = productId,
                Product = product,
                Quantity = quantity
            });
        }
        
        SaveCart(cart);
    }
    
    public async Task UpdateQuantityAsync(int productId, int quantity)
    {
        var cart = await GetCartAsync();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        
        if (item != null)
        {
            if (quantity <= 0)
            {
                cart.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }
            
            SaveCart(cart);
        }
    }
    
    public async Task RemoveFromCartAsync(int productId)
    {
        var cart = await GetCartAsync();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        
        if (item != null)
        {
            cart.Remove(item);
            SaveCart(cart);
        }
    }
    
    public async Task ClearCartAsync()
    {
        var session = _httpContextAccessor.HttpContext.Session;
        session.Remove(CartSessionKey);
    }
    
    public async Task<int> GetCartCountAsync()
    {
        var cart = await GetCartAsync();
        return cart.Sum(i => i.Quantity);
    }
    
    public async Task<decimal> GetCartTotalAsync()
    {
        var cart = await GetCartAsync();
        return cart.Sum(i => i.Quantity * i.Product.Price);
    }
    
    private void SaveCart(List<CartItem> cart)
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var cartJson = JsonSerializer.Serialize(cart.Select(i => new 
        {
            i.ProductId,
            i.Quantity
        }));
        session.SetString(CartSessionKey, cartJson);
    }
}
```

### Cart Model

```csharp
public class CartItem
{
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal Total => Quantity * Product.Price;
}
```

### Cart Page

**Cart/Index.cshtml.cs:**
```csharp
public class IndexModel : PageModel
{
    private readonly ICartService _cartService;
    
    public List<CartItem> CartItems { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    
    public IndexModel(ICartService cartService)
    {
        _cartService = cartService;
    }
    
    public async Task OnGetAsync()
    {
        CartItems = await _cartService.GetCartAsync();
        CalculateTotals();
    }
    
    public async Task<IActionResult> OnPostUpdateQuantityAsync(int productId, int quantity)
    {
        await _cartService.UpdateQuantityAsync(productId, quantity);
        return RedirectToPage();
    }
    
    public async Task<IActionResult> OnPostRemoveAsync(int productId)
    {
        await _cartService.RemoveFromCartAsync(productId);
        TempData["Success"] = "Item removed from cart";
        return RedirectToPage();
    }
    
    private void CalculateTotals()
    {
        Subtotal = CartItems.Sum(i => i.Total);
        Tax = Subtotal * 0.0825m; // 8.25% tax
        ShippingCost = Subtotal > 50 ? 0 : 5.99m; // Free shipping over $50
        Total = Subtotal + Tax + ShippingCost;
    }
}
```

**Cart/Index.cshtml:**
```html
@page
@model IndexModel

<div class="container py-5">
    <h1 class="mb-4">Shopping Cart</h1>
    
    @if (!Model.CartItems.Any())
    {
        <div class="alert alert-info">
            Your cart is empty. <a asp-page="/Products/Index">Continue shopping</a>
        </div>
    }
    else
    {
        <div class="row">
            <div class="col-lg-8">
                @foreach (var item in Model.CartItems)
                {
                    <div class="card mb-3">
                        <div class="row g-0">
                            <div class="col-md-3">
                                <img src="@item.Product.ImageUrl" class="img-fluid" alt="@item.Product.Name">
                            </div>
                            <div class="col-md-9">
                                <div class="card-body">
                                    <h5 class="card-title">@item.Product.Name</h5>
                                    <p class="text-muted">@item.Product.Category - @item.Product.SubCategory</p>
                                    <p class="card-text">$@item.Product.Price.ToString("0.00")</p>
                                    
                                    <div class="d-flex align-items-center gap-3">
                                        <form method="post" asp-page-handler="UpdateQuantity">
                                            <input type="hidden" name="productId" value="@item.ProductId" />
                                            <div class="input-group" style="width: 150px;">
                                                <button class="btn btn-outline-secondary" type="submit" 
                                                        name="quantity" value="@(item.Quantity - 1)">-</button>
                                                <input type="number" class="form-control text-center" 
                                                       value="@item.Quantity" readonly>
                                                <button class="btn btn-outline-secondary" type="submit" 
                                                        name="quantity" value="@(item.Quantity + 1)">+</button>
                                            </div>
                                        </form>
                                        
                                        <form method="post" asp-page-handler="Remove">
                                            <input type="hidden" name="productId" value="@item.ProductId" />
                                            <button type="submit" class="btn btn-link text-danger">Remove</button>
                                        </form>
                                    </div>
                                    
                                    <p class="mt-2 mb-0">
                                        <strong>Subtotal: $@item.Total.ToString("0.00")</strong>
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                }
            </div>
            
            <div class="col-lg-4">
                <div class="card">
                    <div class="card-body">
                        <h5 class="card-title">Order Summary</h5>
                        <hr>
                        <div class="d-flex justify-content-between mb-2">
                            <span>Subtotal:</span>
                            <span>$@Model.Subtotal.ToString("0.00")</span>
                        </div>
                        <div class="d-flex justify-content-between mb-2">
                            <span>Tax (8.25%):</span>
                            <span>$@Model.Tax.ToString("0.00")</span>
                        </div>
                        <div class="d-flex justify-content-between mb-2">
                            <span>Shipping:</span>
                            <span>
                                @if (Model.ShippingCost == 0)
                                {
                                    <span class="badge bg-success">FREE</span>
                                }
                                else
                                {
                                    @:$@Model.ShippingCost.ToString("0.00")
                                }
                            </span>
                        </div>
                        <hr>
                        <div class="d-flex justify-content-between mb-3">
                            <strong>Total:</strong>
                            <strong>$@Model.Total.ToString("0.00")</strong>
                        </div>
                        
                        <a asp-page="/Checkout/Index" class="btn btn-primary w-100 mb-2">
                            Proceed to Checkout
                        </a>
                        <a asp-page="/Products/Index" class="btn btn-outline-secondary w-100">
                            Continue Shopping
                        </a>
                    </div>
                </div>
            </div>
        </div>
    }
</div>
```

### Add to Cart Button

**On Product Pages:**
```html
<form method="post" asp-page-handler="AddToCart">
    <input type="hidden" name="productId" value="@Model.Product.Id" />
    <div class="input-group mb-3">
        <input type="number" name="quantity" value="1" min="1" max="@Model.Product.StockQuantity" 
               class="form-control" style="max-width: 100px;">
        <button type="submit" class="btn btn-primary">
            <i class="bi bi-cart-plus"></i> Add to Cart
        </button>
    </div>
</form>
```

```csharp
public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity)
{
    await _cartService.AddToCartAsync(productId, quantity);
    TempData["Success"] = "Product added to cart!";
    return RedirectToPage();
}
```

### Cart Badge (Navigation)

**Shared/_Layout.cshtml:**
```html
<li class="nav-item position-relative">
    <a class="nav-link" asp-page="/Cart/Index">
        <i class="bi bi-cart"></i> Cart
        @if (ViewBag.CartCount > 0)
        {
            <span class="badge bg-danger position-absolute top-0 start-100 translate-middle cart-badge">
                @ViewBag.CartCount
            </span>
        }
    </a>
</li>
```

**_ViewStart.cshtml:**
```csharp
@inject ICartService CartService

@{
    ViewBag.CartCount = await CartService.GetCartCountAsync();
}
```

---

## ?? Product Management

### Product Model

```csharp
public class Product
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; }
    
    [Required]
    public string Description { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Category { get; set; }
    
    [StringLength(50)]
    public string SubCategory { get; set; }
    
    [Required]
    [Range(0.01, 10000.00)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    [Required]
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
    
    public string ImageUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<OrderItem> OrderItems { get; set; }
    
    // Computed properties
    public bool IsLowStock => StockQuantity <= 10;
    public bool IsOutOfStock => StockQuantity == 0;
}
```

### Product List Page

**Products/Index.cshtml.cs:**
```csharp
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public List<Product> Products { get; set; }
    public string CurrentCategory { get; set; }
    public string CurrentSubCategory { get; set; }
    
    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task OnGetAsync(string category, string subcategory)
    {
        CurrentCategory = category;
        CurrentSubCategory = subcategory;
        
        IQueryable<Product> query = _context.Products;
        
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
        }
        
        if (!string.IsNullOrEmpty(subcategory))
        {
            query = query.Where(p => p.SubCategory == subcategory);
        }
        
        Products = await query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }
}
```

### Product Details Page

**Products/Details.cshtml.cs:**
```csharp
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cartService;
    
    public Product Product { get; set; }
    public List<Product> RelatedProducts { get; set; }
    
    [BindProperty]
    public int Quantity { get; set; } = 1;
    
    public DetailsModel(ApplicationDbContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }
    
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Product = await _context.Products.FindAsync(id);
        
        if (Product == null)
        {
            return NotFound();
        }
        
        // Get related products (same category)
        RelatedProducts = await _context.Products
            .Where(p => p.Category == Product.Category && p.Id != Product.Id)
            .Take(4)
            .ToListAsync();
        
        return Page();
    }
    
    public async Task<IActionResult> OnPostAddToCartAsync(int id, int quantity)
    {
        await _cartService.AddToCartAsync(id, quantity);
        TempData["Success"] = "Product added to cart!";
        return RedirectToPage();
    }
}
```

### Database Seed

Products are automatically seeded on first run:

```csharp
// Data/SeedData.cs
if (!await context.Products.AnyAsync())
{
    var products = new List<Product>
    {
        new Product
        {
            Name = "Mushroom T-Shirt",
            Description = "100% cotton t-shirt with mushroom design",
            Price = 24.99M,
            Category = "Apparel",
            SubCategory = "Tshirts",
            ImageUrl = "/images/products/mushroom-tshirt.svg",
            StockQuantity = 100
        },
        new Product
        {
            Name = "Fungi Cap",
            Description = "Adjustable cap with embroidered mushroom logo",
            Price = 19.99M,
            Category = "Apparel",
            SubCategory = "Hats",
            ImageUrl = "/images/products/fungi-cap.svg",
            StockQuantity = 75
        },
        // More products...
    };
    
    context.Products.AddRange(products);
    await context.SaveChangesAsync();
}
```

---

## ?? Order System

### Order Models

```csharp
public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    
    // Customer Information
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    
    // Shipping Information
    public string ShippingAddress { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    
    // Order Details
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Tax { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; set; }
    
    // Payment
    public string PaymentMethod { get; set; }
    public string StripePaymentIntentId { get; set; }
    public string StripeCustomerId { get; set; }
    
    // Navigation
    public ICollection<OrderItem> OrderItems { get; set; }
    public ICollection<OrderTimeline> OrderTimeline { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    
    public int ProductId { get; set; }
    public Product Product { get; set; }
    
    public int Quantity { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Total => Quantity * UnitPrice;
}

public class OrderTimeline
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string Notes { get; set; }
}
```

### Order Creation

**Checkout/Payment.cshtml.cs:**
```csharp
public async Task<IActionResult> OnPostAsync()
{
    if (!ModelState.IsValid)
    {
        return Page();
    }
    
    var cartItems = await _cartService.GetCartAsync();
    
    // Calculate totals
    var subtotal = cartItems.Sum(i => i.Total);
    var tax = subtotal * 0.0825m;
    var shipping = subtotal > 50 ? 0 : 5.99m;
    var total = subtotal + tax + shipping;
    
    // Create order
    var order = new Order
    {
        UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        PhoneNumber = PhoneNumber,
        ShippingAddress = ShippingAddress,
        City = City,
        State = State,
        ZipCode = ZipCode,
        Subtotal = subtotal,
        Tax = tax,
        ShippingCost = shipping,
        Total = total,
        Status = "Pending",
        OrderDate = DateTime.UtcNow
    };
    
    // Add order items
    foreach (var item in cartItems)
    {
        order.OrderItems.Add(new OrderItem
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.Product.Price
        });
        
        // Update stock
        item.Product.StockQuantity -= item.Quantity;
    }
    
    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    
    // Add initial timeline entry
    order.OrderTimeline.Add(new OrderTimeline
    {
        Status = "Placed",
        Timestamp = DateTime.UtcNow,
        Notes = "Order placed successfully"
    });
    
    await _context.SaveChangesAsync();
    
    // Create Stripe Payment Intent
    var paymentIntent = await _paymentService.CreatePaymentIntentAsync(
        amount: total,
        currency: "usd",
        metadata: new Dictionary<string, string>
        {
            { "order_id", order.Id.ToString() },
            { "customer_email", order.Email }
        }
    );
    
    order.StripePaymentIntentId = paymentIntent.Id;
    await _context.SaveChangesAsync();
    
    // Clear cart
    await _cartService.ClearCartAsync();
    
    return RedirectToPage("/Checkout/Complete", new { orderId = order.Id });
}
```

### Order Status Updates

```csharp
public async Task UpdateOrderStatusAsync(int orderId, string newStatus, string notes = null)
{
    var order = await _context.Orders
        .Include(o => o.OrderTimeline)
        .FirstOrDefaultAsync(o => o.Id == orderId);
    
    if (order == null) return;
    
    order.Status = newStatus;
    order.UpdatedAt = DateTime.UtcNow;
    
    order.OrderTimeline.Add(new OrderTimeline
    {
        Status = newStatus,
        Timestamp = DateTime.UtcNow,
        Notes = notes
    });
    
    _context.Update(order);
    await _context.SaveChangesAsync();
    
    // Send email notification
    await SendOrderStatusEmail(order);
}
```

---

## ?? Sales Tax

### Implementation

**Fixed Rate: 8.25%** (Texas state tax)

```csharp
public class TaxService
{
    private const decimal TaxRate = 0.0825m; // 8.25%
    
    public decimal CalculateTax(decimal subtotal)
    {
        return Math.Round(subtotal * TaxRate, 2);
    }
    
    public decimal CalculateTotalWithTax(decimal subtotal)
    {
        var tax = CalculateTax(subtotal);
        return subtotal + tax;
    }
}
```

### Usage in Cart/Checkout

```csharp
// Calculate tax
public decimal Subtotal { get; set; }
public decimal Tax => Math.Round(Subtotal * 0.0825m, 2);
public decimal Total => Subtotal + Tax + ShippingCost;
```

### Display

```html
<div class="d-flex justify-content-between">
    <span>Subtotal:</span>
    <span>$@Model.Subtotal.ToString("0.00")</span>
</div>
<div class="d-flex justify-content-between">
    <span>Tax (8.25%):</span>
    <span>$@Model.Tax.ToString("0.00")</span>
</div>
<div class="d-flex justify-content-between">
    <span>Shipping:</span>
    <span>$@Model.ShippingCost.ToString("0.00")</span>
</div>
<hr>
<div class="d-flex justify-content-between">
    <strong>Total:</strong>
    <strong>$@Model.Total.ToString("0.00")</strong>
</div>
```

---

## ?? Customer Order History

### Customer Orders Page

**Orders/Index.cshtml.cs:**
```csharp
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public List<Order> Orders { get; set; }
    
    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        Orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}
```

### Order Details Page

**Orders/Details.cshtml.cs:**
```csharp
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    public Order Order { get; set; }
    
    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        Order = await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderTimeline)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        
        if (Order == null)
        {
            return NotFound();
        }
        
        return Page();
    }
}
```

### Order Timeline Display

```html
<div class="order-timeline">
    <h5>Order Timeline</h5>
    @foreach (var event in Model.Order.OrderTimeline.OrderBy(t => t.Timestamp))
    {
        <div class="timeline-event">
            <div class="timeline-marker @GetStatusClass(event.Status)"></div>
            <div class="timeline-content">
                <strong>@event.Status</strong>
                <p class="text-muted mb-1">@event.Timestamp.ToString("MMM dd, yyyy 'at' h:mm tt")</p>
                @if (!string.IsNullOrEmpty(event.Notes))
                {
                    <p class="mb-0">@event.Notes</p>
                }
            </div>
        </div>
    }
</div>

@functions {
    string GetStatusClass(string status)
    {
        return status switch
        {
            "Placed" => "bg-primary",
            "Paid" => "bg-success",
            "Processing" => "bg-info",
            "Shipped" => "bg-warning",
            "Delivered" => "bg-success",
            "Cancelled" => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
```

```css
.order-timeline {
    position: relative;
    padding-left: 30px;
}

.order-timeline::before {
    content: '';
    position: absolute;
    left: 10px;
    top: 0;
    bottom: 0;
    width: 2px;
    background: var(--gray-300);
}

.timeline-event {
    position: relative;
    margin-bottom: 1.5rem;
}

.timeline-marker {
    position: absolute;
    left: -25px;
    width: 12px;
    height: 12px;
    border-radius: 50%;
    border: 2px solid white;
}

.timeline-content {
    padding-left: 10px;
}
```

---

## ? Feature Checklist

### Shopping Cart
- [x] Session-based storage
- [x] Add items with quantity
- [x] Update quantities
- [x] Remove items
- [x] Calculate totals with tax
- [x] Free shipping over $50
- [x] Cart badge in navigation
- [x] Toast notifications

### Products
- [x] Product list with filtering
- [x] Product details page
- [x] Related products
- [x] Stock management
- [x] Low stock alerts
- [x] Image support
- [x] Database seeding

### Orders
- [x] Order creation
- [x] Order status tracking
- [x] Order timeline
- [x] Customer order history
- [x] Admin order management
- [x] Email notifications
- [x] Stripe integration

### Sales Tax
- [x] 8.25% tax rate
- [x] Calculated on checkout
- [x] Displayed in cart
- [x] Stored with order

---

**Everything consolidated from:**
- SHOPPING_CART_README.md
- PRODUCT_DETAILS_PAGE.md
- CUSTOMER_ORDER_DETAILS.md
- SALES_TAX_IMPLEMENTATION.md
- ORDER_SUMMARY_FIX.md
- CANCELLED_ORDER_TIMELINE_FIX.md
- SQL_TRANSLATION_FIX.md
- PRODUCTS_DATABASE_UPDATE.md
