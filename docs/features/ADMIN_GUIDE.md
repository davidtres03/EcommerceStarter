# ????? Admin Panel Guide
## MyStore Supply Co.

Complete guide for the admin panel, including user management, inventory, orders, and payment configurations.

---

## ?? Table of Contents

1. [Admin Panel Overview](#admin-panel-overview)
2. [Getting Started](#getting-started)
3. [Dashboard](#dashboard)
4. [User Management](#user-management)
5. [Product & Inventory Management](#product--inventory-management)
6. [Order Management](#order-management)
7. [Payment Configuration](#payment-configuration)
8. [Reports & Analytics](#reports--analytics)

---

## ?? Admin Panel Overview

The admin panel provides comprehensive tools to manage your e-commerce site:

### Features
- ? Dashboard with key metrics
- ? User management (customers and admins)
- ? Product catalog management
- ? Inventory tracking
- ? Order processing and tracking
- ? Payment method configuration
- ? Customer details and history
- ? Sales reports

### Access
```
URL: /Admin/Dashboard
Login: admin@example.com
Default Password: Admin@123
```

?? **IMPORTANT:** Change the default password immediately after first login!

---

## ?? Getting Started

### First-Time Setup

1. **Login as Admin**
   ```
   Navigate to: /Account/Login
   Email: admin@example.com
   Password: Admin@123
   ```

2. **Change Default Password**
   ```
   Click your name ? Account Settings ? Change Password
   Use a strong password (12+ characters, mixed case, numbers, symbols)
   ```

3. **Verify Dashboard Access**
   ```
   Top navigation ? "Admin Panel" link should be visible
   Click it to access /Admin/Dashboard
   ```

### Admin User Creation

**Via Seed Data (Automatic):**
```csharp
// Data/SeedData.cs - Already configured
public static async Task Initialize(IServiceProvider serviceProvider)
{
    // Admin user is created on first run
    var adminEmail = "admin@example.com";
    var adminUser = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        FirstName = "Admin",
        LastName = "User",
        EmailConfirmed = true
    };
    
    await userManager.CreateAsync(adminUser, "Admin@123");
    await userManager.AddToRoleAsync(adminUser, "Admin");
}
```

**Manually Create Admin:**
```csharp
// Option 1: Via code
var user = new ApplicationUser
{
    UserName = "newadmin@example.com",
    Email = "newadmin@example.com",
    FirstName = "New",
    LastName = "Admin",
    EmailConfirmed = true
};

await _userManager.CreateAsync(user, "SecurePassword123!");
await _userManager.AddToRoleAsync(user, "Admin");

// Option 2: Via SQL
INSERT INTO AspNetUsers (Id, UserName, Email, FirstName, LastName, EmailConfirmed)
VALUES (NEWID(), 'admin@example.com', 'admin@example.com', 'Admin', 'User', 1);

INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id 
FROM AspNetUsers u, AspNetRoles r 
WHERE u.Email = 'admin@example.com' AND r.Name = 'Admin';
```

---

## ?? Dashboard

### Overview Cards

```
???????????????????????????????????????????????????????????
?  ?? Products          ?? Customers      ?? Orders       ?
?      247                 1,234            567           ?
?  ????????????      ????????????      ??????????         ?
?                                                         ?
?  ?? Total Revenue                                       ?
?      $45,678.90                                         ?
???????????????????????????????????????????????????????????
```

### Quick Stats

**Products Card:**
- Total number of products in catalog
- Link to manage products
- Color-coded (blue)

**Customers Card:**
- Total registered customers
- Link to customer management
- Color-coded (green)

**Orders Card:**
- Total orders placed
- Link to order management
- Color-coded (amber)

**Revenue Card:**
- Total revenue generated
- Calculated from all successful orders
- Color-coded (purple)

### Quick Actions

- **Manage Products** ? `/Admin/Products/Index`
- **View Orders** ? `/Admin/Orders/Index`
- **Manage Customers** ? `/Admin/Customers/Index`

---

## ?? User Management

### Customer List

**Access:** `/Admin/Customers/Index`

**Features:**
- View all registered customers
- Search by name or email
- Filter by registration date
- View total orders per customer
- Access customer details

**Table Columns:**
```
| ID | Name | Email | Phone | Registration Date | Total Orders | Actions |
|----|------|-------|-------|-------------------|--------------|---------|
```

### Customer Details

**Access:** `/Admin/Customers/Details?id={customerId}`

**Customer Information:**
```
???????????????????????????????????????????
?  Customer Details                       ?
???????????????????????????????????????????
?  Name:           John Doe               ?
?  Email:          john@example.com       ?
?  Phone:          (555) 123-4567         ?
?  Joined:         Jan 15, 2024           ?
?  Total Orders:   12                     ?
?  Lifetime Value: $1,245.89              ?
???????????????????????????????????????????
```

**Order History:**
- List of all orders by customer
- Order date, total, status
- Quick view order details
- Filter by date range
- Export to CSV

**Shipping Addresses:**
- All saved addresses
- Primary address highlighted
- Edit/delete capabilities

**Account Status:**
- Active/Suspended/Banned
- Email verified status
- Last login date
- Account creation date

### User Management Actions

**View All Users:**
```csharp
public async Task<IActionResult> OnGetAsync(string searchString, string sortOrder)
{
    IQueryable<ApplicationUser> usersQuery = _context.Users;
    
    // Search
    if (!string.IsNullOrEmpty(searchString))
    {
        usersQuery = usersQuery.Where(u => 
            u.FirstName.Contains(searchString) ||
            u.LastName.Contains(searchString) ||
            u.Email.Contains(searchString));
    }
    
    // Sort
    usersQuery = sortOrder switch
    {
        "name_desc" => usersQuery.OrderByDescending(u => u.FirstName),
        "email" => usersQuery.OrderBy(u => u.Email),
        "date" => usersQuery.OrderBy(u => u.CreatedAt),
        _ => usersQuery.OrderBy(u => u.FirstName)
    };
    
    Users = await usersQuery.ToListAsync();
    return Page();
}
```

**View User Details:**
```csharp
public async Task<IActionResult> OnGetAsync(string id)
{
    User = await _userManager.FindByIdAsync(id);
    
    if (User == null)
    {
        return NotFound();
    }
    
    // Get user's orders
    Orders = await _context.Orders
        .Where(o => o.UserId == id)
        .OrderByDescending(o => o.OrderDate)
        .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
        .ToListAsync();
    
    // Calculate totals
    TotalOrders = Orders.Count;
    TotalSpent = Orders.Sum(o => o.Total);
    
    return Page();
}
```

### Promote User to Admin

```csharp
public async Task<IActionResult> OnPostPromoteToAdminAsync(string userId)
{
    var user = await _userManager.FindByIdAsync(userId);
    
    if (user == null)
    {
        return NotFound();
    }
    
    // Check if already admin
    var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
    
    if (!isAdmin)
    {
        await _userManager.AddToRoleAsync(user, "Admin");
        TempData["Success"] = $"User {user.Email} promoted to Admin.";
    }
    else
    {
        TempData["Warning"] = "User is already an Admin.";
    }
    
    return RedirectToPage();
}
```

---

## ?? Product & Inventory Management

### Product List

**Access:** `/Admin/Products/Index`

**Features:**
- View all products
- Search by name/category
- Filter by stock status
- Sort by various fields
- Quick edit/delete

**Table View:**
```
| Image | Name | Category | Price | Stock | Status | Actions |
|-------|------|----------|-------|-------|--------|---------|
| ???   | Cap  | Hats     | $19.99| 50    | Active | Edit Delete |
```

### Add New Product

**Access:** `/Admin/Products/Create`

**Form Fields:**
```
Product Name:        [_____________________]
Description:         [_____________________]
                     [                     ]
Category:            [Dropdown: Apparel ?]
SubCategory:         [Dropdown: Hats ?   ]
Price:               [$____.__]
Stock Quantity:      [_____]
Image URL:           [_____________________]
                     
[Cancel] [Create Product]
```

**Validation:**
- Name: Required, max 200 characters
- Description: Required
- Category: Required
- Price: Required, must be > 0
- Stock: Required, must be >= 0
- Image URL: Optional

**Example:**
```csharp
public async Task<IActionResult> OnPostAsync()
{
    if (!ModelState.IsValid)
    {
        return Page();
    }
    
    var product = new Product
    {
        Name = ProductName,
        Description = Description,
        Category = Category,
        SubCategory = SubCategory,
        Price = Price,
        StockQuantity = StockQuantity,
        ImageUrl = ImageUrl ?? "/images/products/default.svg",
        CreatedAt = DateTime.UtcNow
    };
    
    _context.Products.Add(product);
    await _context.SaveChangesAsync();
    
    TempData["Success"] = "Product created successfully!";
    return RedirectToPage("./Index");
}
```

### Edit Product

**Access:** `/Admin/Products/Edit?id={productId}`

**Pre-filled form with current values**
- Update any field
- Changes saved immediately
- Stock adjustments tracked

### Inventory Management

**Stock Tracking:**
```csharp
public class Product
{
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public bool IsLowStock => StockQuantity <= ReorderLevel;
    public bool IsOutOfStock => StockQuantity == 0;
}
```

**Stock Alerts:**
```
Low Stock Items:
???????????????????????????????????????
? ?? Mushroom T-Shirt     (5 left)   ?
? ?? Fungi Cap           (8 left)    ?
? ? Forest Hoodie       (0 left)    ?
???????????????????????????????????????
```

**Stock Adjustment:**
```csharp
public async Task<IActionResult> OnPostAdjustStockAsync(int productId, int adjustment)
{
    var product = await _context.Products.FindAsync(productId);
    
    if (product == null)
    {
        return NotFound();
    }
    
    product.StockQuantity += adjustment;
    
    if (product.StockQuantity < 0)
    {
        product.StockQuantity = 0;
    }
    
    _context.Update(product);
    await _context.SaveChangesAsync();
    
    // Log the adjustment
    _context.InventoryLogs.Add(new InventoryLog
    {
        ProductId = productId,
        Adjustment = adjustment,
        NewQuantity = product.StockQuantity,
        Reason = "Manual adjustment",
        AdjustedBy = User.Identity.Name,
        AdjustedAt = DateTime.UtcNow
    });
    
    await _context.SaveChangesAsync();
    
    return RedirectToPage();
}
```

### Inventory Reports

**Low Stock Report:**
```csharp
var lowStockProducts = await _context.Products
    .Where(p => p.StockQuantity <= p.ReorderLevel)
    .OrderBy(p => p.StockQuantity)
    .ToListAsync();
```

**Out of Stock Report:**
```csharp
var outOfStockProducts = await _context.Products
    .Where(p => p.StockQuantity == 0)
    .ToListAsync();
```

**Stock Movement Report:**
```csharp
var stockMovement = await _context.InventoryLogs
    .Where(l => l.AdjustedAt >= startDate && l.AdjustedAt <= endDate)
    .Include(l => l.Product)
    .OrderByDescending(l => l.AdjustedAt)
    .ToListAsync();
```

---

## ?? Order Management

### Order List

**Access:** `/Admin/Orders/Index`

**Features:**
- View all orders
- Filter by status
- Search by order number or customer
- Sort by date/total
- Quick status updates

**Status Badges:**
```
?? Pending      - Payment received, awaiting processing
?? Processing   - Order being prepared
?? Shipped      - Out for delivery
?? Delivered    - Successfully delivered
?? Cancelled    - Order cancelled
```

**Table View:**
```
| Order # | Customer | Date | Total | Status | Actions |
|---------|----------|------|-------|--------|---------|
| #1001   | John Doe | 3/15 | $45.99| Shipped| View Edit |
```

### Order Details

**Access:** `/Admin/Orders/Details?id={orderId}`

**Order Information:**
```
????????????????????????????????????????????????
?  Order #1001                                 ?
????????????????????????????????????????????????
?  Status: ?? Processing                       ?
?  Date: March 15, 2024 at 2:30 PM            ?
?  Payment: Paid via Card (****4242)          ?
????????????????????????????????????????????????
?  Customer Information                        ?
?  Name: John Doe                              ?
?  Email: john@example.com                     ?
?  Phone: (555) 123-4567                       ?
????????????????????????????????????????????????
?  Shipping Address                            ?
?  123 Main Street                             ?
?  Austin, TX 78701                            ?
????????????????????????????????????????????????
?  Order Items                                 ?
?  ? Mushroom T-Shirt (2x)      $49.98        ?
?  ? Fungi Cap (1x)              $19.99        ?
?                                              ?
?  Subtotal:                     $69.97        ?
?  Tax (8.25%):                   $5.77        ?
?  Shipping:                      $5.99        ?
?  ?????????????????????????????????????       ?
?  Total:                        $81.73        ?
????????????????????????????????????????????????
?  Order Timeline                              ?
?  ?? Placed       3/15 at 2:30 PM            ?
?  ?? Paid         3/15 at 2:31 PM            ?
?  ?? Processing   3/15 at 3:00 PM            ?
?  ? Shipped       Pending                    ?
?  ? Delivered     Pending                    ?
????????????????????????????????????????????????
```

### Update Order Status

```csharp
public async Task<IActionResult> OnPostUpdateStatusAsync(int orderId, string newStatus)
{
    var order = await _context.Orders.FindAsync(orderId);
    
    if (order == null)
    {
        return NotFound();
    }
    
    order.Status = newStatus;
    order.UpdatedAt = DateTime.UtcNow;
    
    _context.Update(order);
    await _context.SaveChangesAsync();
    
    // Send email notification to customer
    await _emailService.SendOrderStatusUpdateEmail(order);
    
    TempData["Success"] = $"Order status updated to {newStatus}";
    return RedirectToPage("./Details", new { id = orderId });
}
```

### Order Timeline

**Implementation:**
```csharp
public class OrderTimeline
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string Notes { get; set; }
}

// Track status changes
public async Task AddTimelineEventAsync(int orderId, string status, string notes = null)
{
    _context.OrderTimelines.Add(new OrderTimeline
    {
        OrderId = orderId,
        Status = status,
        Timestamp = DateTime.UtcNow,
        Notes = notes
    });
    
    await _context.SaveChangesAsync();
}
```

### Bulk Actions

**Update Multiple Orders:**
```csharp
public async Task<IActionResult> OnPostBulkUpdateAsync(List<int> orderIds, string action)
{
    var orders = await _context.Orders
        .Where(o => orderIds.Contains(o.Id))
        .ToListAsync();
    
    foreach (var order in orders)
    {
        switch (action)
        {
            case "mark-shipped":
                order.Status = "Shipped";
                break;
            case "mark-delivered":
                order.Status = "Delivered";
                break;
            case "cancel":
                order.Status = "Cancelled";
                break;
        }
        
        order.UpdatedAt = DateTime.UtcNow;
    }
    
    await _context.SaveChangesAsync();
    
    TempData["Success"] = $"{orders.Count} orders updated successfully";
    return RedirectToPage();
}
```

---

## ?? Payment Configuration

### Stripe Settings

**Access:** `/Admin/Settings/Payment`

**Configuration Options:**

1. **API Keys**
   ```
   Test Mode:
   ? Enabled (for development)
   
   Publishable Key: pk_test_****
   Secret Key:      sk_test_**** [Hidden]
   
   [Update Keys]
   ```

2. **Payment Methods**
   ```
   ? Credit/Debit Cards ? CashApp Pay ? Google Pay ? Apple Pay ? Link (by Stripe)
   ```

3. **Currency**
   ```
   Default Currency: USD ($)
   Supported: USD, EUR, GBP
   ```

### Payment Method Management

**Enable/Disable Methods:**
```csharp
public async Task<IActionResult> OnPostTogglePaymentMethodAsync(string methodName, bool enabled)
{
    var setting = await _context.PaymentSettings
        .FirstOrDefaultAsync(s => s.MethodName == methodName);
    
    if (setting == null)
    {
        setting = new PaymentSetting
        {
            MethodName = methodName,
            IsEnabled = enabled
        };
        _context.PaymentSettings.Add(setting);
    }
    else
    {
        setting.IsEnabled = enabled;
        _context.PaymentSettings.Update(setting);
    }
    
    await _context.SaveChangesAsync();
    
    TempData["Success"] = $"{methodName} {(enabled ? "enabled" : "disabled")}";
    return RedirectToPage();
}
```

### Test Mode Toggle

**Switch Between Test/Live:**
```csharp
public async Task<IActionResult> OnPostToggleTestModeAsync(bool testMode)
{
    var config = await _context.SiteConfigurations.FirstAsync();
    config.StripeTestMode = testMode;
    
    _context.Update(config);
    await _context.SaveChangesAsync();
    
    // Update Stripe.ApiKey
    StripeConfiguration.ApiKey = testMode ? _configuration["Stripe:TestSecretKey"]
        : _configuration["Stripe:LiveSecretKey"];
    
    TempData["Warning"] = testMode 
        ? "?? Test mode enabled - No real charges will be made"
        : "?? LIVE mode enabled - Real charges will be processed!";
    
    return RedirectToPage();
}
```

### Payment Logs

**View Transaction History:**
```
| Date | Order | Customer | Amount | Method | Status | Stripe ID |
|------|-------|----------|--------|--------|--------|-----------|
| 3/15 | #1001 | John Doe | $81.73 | Card   | ? Paid| pi_123... |
```

**Export to CSV:**
```csharp
public async Task<IActionResult> OnPostExportPaymentLogsAsync(DateTime startDate, DateTime endDate)
{
    var payments = await _context.Orders
        .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
        .Select(o => new
        {
            o.Id,
            o.OrderDate,
            Customer = $"{o.FirstName} {o.LastName}",
            o.Total,
            o.PaymentMethod,
            o.StripePaymentIntentId,
            o.Status
        })
        .ToListAsync();
    
    var csv = GenerateCsv(payments);
    return File(Encoding.UTF8.GetBytes(csv), "text/csv", "payment-logs.csv");
}
```

---

## ?? Reports & Analytics

### Sales Reports

**Daily Sales:**
```csharp
var dailySales = await _context.Orders
    .Where(o => o.OrderDate >= DateTime.Today)
    .Where(o => o.Status != "Cancelled")
    .SumAsync(o => o.Total);
```

**Monthly Sales:**
```csharp
var monthlySales = await _context.Orders
    .Where(o => o.OrderDate.Month == DateTime.Now.Month)
    .Where(o => o.Status != "Cancelled")
    .GroupBy(o => o.OrderDate.Date)
    .Select(g => new
    {
        Date = g.Key,
        Total = g.Sum(o => o.Total),
        Count = g.Count()
    })
    .ToListAsync();
```

**Top Products:**
```csharp
var topProducts = await _context.OrderItems
    .GroupBy(oi => oi.ProductId)
    .Select(g => new
    {
        ProductId = g.Key,
        ProductName = g.First().Product.Name,
        TotalQuantity = g.Sum(oi => oi.Quantity),
        TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
    })
    .OrderByDescending(x => x.TotalRevenue)
    .Take(10)
    .ToListAsync();
```

### Customer Analytics

**New Customers (This Month):**
```csharp
var newCustomers = await _context.Users
    .Where(u => u.CreatedAt.Month == DateTime.Now.Month)
    .CountAsync();
```

**Repeat Customers:**
```csharp
var repeatCustomers = await _context.Orders
    .GroupBy(o => o.UserId)
    .Where(g => g.Count() > 1)
    .CountAsync();
```

**Customer Lifetime Value:**
```csharp
var customerLTV = await _context.Orders
    .Where(o => o.Status != "Cancelled")
    .GroupBy(o => o.UserId)
    .Select(g => new
    {
        UserId = g.Key,
        CustomerName = g.First().FirstName + " " + g.First().LastName,
        TotalOrders = g.Count(),
        TotalSpent = g.Sum(o => o.Total),
        AverageOrder = g.Average(o => o.Total)
    })
    .OrderByDescending(x => x.TotalSpent)
    .ToListAsync();
```

### Dashboard Metrics

**Key Performance Indicators:**
```csharp
public class DashboardMetrics
{
    public decimal TodaySales { get; set; }
    public decimal MonthSales { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int PendingOrders { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public async Task<DashboardMetrics> GetDashboardMetricsAsync()
{
    return new DashboardMetrics
    {
        TodaySales = await GetTodaySalesAsync(),
        MonthSales = await GetMonthSalesAsync(),
        TotalProducts = await _context.Products.CountAsync(),
        LowStockProducts = await _context.Products.CountAsync(p => p.IsLowStock),
        PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending"),
        TotalCustomers = await _context.Users.CountAsync(),
        NewCustomersThisMonth = await GetNewCustomersThisMonthAsync(),
        AverageOrderValue = await GetAverageOrderValueAsync()
    };
}
```

---

## ?? Security & Permissions

### Role-Based Access

**Admin Role Required:**
```csharp
[Authorize(Roles = "Admin")]
public class AdminDashboardModel : PageModel
{
    // Only accessible by admins
}
```

**Check in Razor:**
```razor
@if (User.IsInRole("Admin"))
{
    <a asp-page="/Admin/Dashboard">Admin Panel</a>
}
```

### Audit Logging

**Track Admin Actions:**
```csharp
public async Task LogAdminActionAsync(string action, string details)
{
    _context.AuditLogs.Add(new AuditLog
    {
        UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        UserEmail = User.Identity.Name,
        Action = action,
        Details = details,
        Timestamp = DateTime.UtcNow,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
    });
    
    await _context.SaveChangesAsync();
}
```

---

## ? Admin Checklist

### Daily Tasks
- [ ] Review new orders
- [ ] Check low stock alerts
- [ ] Respond to customer inquiries
- [ ] Update order statuses
- [ ] Review payment logs

### Weekly Tasks
- [ ] Generate sales report
- [ ] Review inventory levels
- [ ] Check for product updates needed
- [ ] Analyze customer feedback
- [ ] Update product images/descriptions

### Monthly Tasks
- [ ] Generate comprehensive reports
- [ ] Review payment settings
- [ ] Analyze top-selling products
- [ ] Plan inventory restocking
- [ ] Review user accounts

---

**Everything consolidated from:**
- ADMIN_PANEL_README.md
- ADMIN_USER_MANAGEMENT.md
- ADMIN_DETAILS_PAGES.md
- ADMIN_PAYMENT_MANAGEMENT_GUIDE.md
- INVENTORY_MANAGEMENT.md
