# ?? Stripe Payment System - Complete Guide
## MyStore Supply Co.

Complete guide for integrating and managing Stripe payments, including multiple payment methods, security, and troubleshooting.

---

## ?? Table of Contents

1. [Quick Setup](#quick-setup)
2. [Getting Your Stripe Keys](#getting-your-stripe-keys)
3. [Secure Key Management](#secure-key-management)
4. [Multiple Payment Methods](#multiple-payment-methods)
5. [Payment Method Selection](#payment-method-selection)
6. [Two-Step Checkout Process](#two-step-checkout-process)
7. [Stripe Customer Integration](#stripe-customer-integration)
8. [Google Pay & Apple Pay](#google-pay--apple-pay)
9. [Troubleshooting](#troubleshooting)
10. [Quick Reference](#quick-reference)

---

## ?? Quick Setup

### Step 1: Get Stripe Account
1. Sign up at https://stripe.com
2. Verify your email
3. Complete business information

### Step 2: Get API Keys
```
Dashboard ? Developers ? API Keys
```
- **Publishable Key**: `pk_test_...` (safe to expose in frontend)
- **Secret Key**: `sk_test_...` (MUST be kept secret)

### Step 3: Configure Application

**For Development (Local Testing):**
```json
// appsettings.Development.json
{
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_KEY_HERE",
    "SecretKey": "sk_test_YOUR_KEY_HERE"
  }
}
```

**For Production (NEVER commit to Git):**
```json
// appsettings.Production.json (add to .gitignore!)
{
  "Stripe": {
    "PublishableKey": "pk_live_YOUR_KEY_HERE",
    "SecretKey": "sk_live_YOUR_KEY_HERE"
  }
}
```

### Step 4: Enable Payment Methods in Stripe Dashboard
```
Dashboard ? Settings ? Payment Methods
```
Enable:
- ? Cards (Visa, Mastercard, Amex)
- ? CashApp Pay
- ? Google Pay
- ? Apple Pay
- ? Link

---

## ?? Getting Your Stripe Keys

### Test Mode Keys (Development)
1. Go to https://dashboard.stripe.com/test/dashboard
2. Click **Developers** in left sidebar
3. Click **API keys**
4. You'll see two keys:
   - **Publishable key**: `pk_test_51...` (starts with `pk_test_`)
   - **Secret key**: Click **Reveal test key** to see `sk_test_...`

### Live Mode Keys (Production)
?? **IMPORTANT**: Only use after thorough testing!

1. Toggle to **Live mode** (top right corner)
2. Go to **Developers** ? **API keys**
3. Get your live keys:
   - **Publishable key**: `pk_live_51...`
   - **Secret key**: `sk_live_...`

### Key Differences

| Feature | Test Keys | Live Keys |
|---------|-----------|-----------|
| Prefix | `pk_test_` / `sk_test_` | `pk_live_` / `sk_live_` |
| Real Charges | ? No | ? Yes |
| Test Cards | ? Works | ? Won't work |
| Dashboard | Separate test data | Real customer data |

### Test Card Numbers
```
Visa (Success):           4242 4242 4242 4242
Visa (Decline):           4000 0000 0000 0002
Mastercard:               5555 5555 5555 4444
Amex:                     3782 822463 10005
Discover:                 6011 1111 1111 1117

Expiry: Any future date (e.g., 12/25)
CVC: Any 3 digits (e.g., 123)
ZIP: Any 5 digits (e.g., 12345)
```

---

## ?? Secure Key Management

### Option 1: User Secrets (Development) ? Recommended

**Why Use User Secrets?**
- ? Keys never stored in source code
- ? Not committed to Git
- ? Easy to manage per developer
- ? Built into .NET

**Setup User Secrets:**
```powershell
# Navigate to project directory
cd EcommerceStarter

# Initialize user secrets
dotnet user-secrets init

# Add Stripe keys
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "Stripe:SecretKey"

# Clear all secrets
dotnet user-secrets clear
```

**Where are secrets stored?**
```
Windows: %APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json
Mac: ~/.microsoft/usersecrets/<user_secrets_id>/secrets.json
Linux: ~/.microsoft/usersecrets/<user_secrets_id>/secrets.json
```

### Option 2: Environment Variables (Production)

**Windows:**
```powershell
# System-wide
[System.Environment]::SetEnvironmentVariable("Stripe__PublishableKey", "pk_live_YOUR_KEY", "Machine")
[System.Environment]::SetEnvironmentVariable("Stripe__SecretKey", "sk_live_YOUR_KEY", "Machine")

# Current user
[System.Environment]::SetEnvironmentVariable("Stripe__PublishableKey", "pk_live_YOUR_KEY", "User")
[System.Environment]::SetEnvironmentVariable("Stripe__SecretKey", "sk_live_YOUR_KEY", "User")
```

**Linux/Mac:**
```bash
export Stripe__PublishableKey="pk_live_YOUR_KEY"
export Stripe__SecretKey="sk_live_YOUR_KEY"

# Add to ~/.bashrc or ~/.zshrc for persistence
echo 'export Stripe__PublishableKey="pk_live_YOUR_KEY"' >> ~/.bashrc
```

### Option 3: Azure Key Vault (Production) ?? Best Practice

**Already configured in your app!**

```csharp
// Program.cs - Already implemented
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVault:VaultUri"]),
    new DefaultAzureCredential());
```

**Setup Azure Key Vault:**
```powershell
# Create Key Vault
az keyvault create \
  --name MyStore-kv \
  --resource-group MyStoreRG \
  --location eastus

# Add secrets
az keyvault secret set \
  --vault-name MyStore-kv \
  --name "Stripe--PublishableKey" \
  --value "pk_live_YOUR_KEY"

az keyvault secret set \
  --vault-name MyStore-kv \
  --name "Stripe--SecretKey" \
  --value "sk_live_YOUR_KEY"

# Grant access to app
az keyvault set-policy \
  --name MyStore-kv \
  --object-id <app-service-principal-id> \
  --secret-permissions get list
```

### Security Best Practices ? 1. **NEVER commit keys to Git**
   ```gitignore
   # .gitignore
   appsettings.Production.json
   appsettings.*.json
   !appsettings.json
   !appsettings.Development.json.template
   ```

2. **Use different keys for environments**
   - Development: Test keys (`pk_test_`, `sk_test_`)
   - Production: Live keys (`pk_live_`, `sk_live_`)

3. **Rotate keys regularly**
   - Stripe Dashboard ? Developers ? API keys ? Roll key

4. **Restrict key permissions**
   - Use restricted API keys when possible
   - Limit to specific actions

5. **Monitor key usage**
   - Review logs in Stripe Dashboard
   - Set up alerts for suspicious activity

---

## ?? Multiple Payment Methods

Your app supports these payment methods:

### 1. Credit/Debit Cards
- Visa, Mastercard, American Express, Discover
- Default payment method
- Works everywhere

### 2. CashApp Pay
- Popular among US customers
- Instant bank-to-bank transfers
- No card required

### 3. Google Pay
- Android users
- Saved cards from Google account
- One-click checkout

### 4. Apple Pay
- iPhone/iPad/Mac users
- Saved cards from Apple Wallet
- Touch ID / Face ID authentication

### 5. Link (Stripe)
- Save payment info with Stripe
- One-click checkout on return visits
- Email-based authentication

### Configuration

**Enable in Stripe Dashboard:**
```
Settings ? Payment methods ? Enable desired methods
```

**Frontend Integration (Already Done):**
```javascript
// Your app automatically supports all enabled payment methods
const paymentElement = elements.create('payment', {
  layout: 'tabs', // Shows all available methods as tabs
  defaultValues: {
    billingDetails: {
      name: customerName,
      email: customerEmail,
      address: { /* ... */ }
    }
  }
});
```

---

## ?? Payment Method Selection

### How It Works

**Step 1: Customer chooses payment method**
```
???????????????????????????????????????????
?  Payment Methods (Tabs)                 ?
???????????????????????????????????????????
?  [Card] [CashApp] [Google Pay] [Link]  ?
?                                         ?
?  Selected Tab Content Shown Below:      ?
?  ? Card: Enter card details            ?
?  ? CashApp: Phone/email                ?
?  ? Google Pay: One-click button        ?
?  ? Link: Email lookup                  ?
???????????????????????????????????????????
```

**Step 2: System validates selection**
```javascript
// Validation happens automatically via Stripe Elements
if (paymentMethod.type === 'card') {
  // Validate card number, expiry, CVC
} else if (paymentMethod.type === 'cashapp') {
  // Validate CashApp account
}
// etc.
```

**Step 3: Payment is processed**
```
Customer confirms ? Stripe processes ? Webhook received ? Order confirmed
```

### Payment Method Storage

**Session-based (Current Order):**
```csharp
// Stored in Payment Intent metadata
var options = new PaymentIntentCreateOptions
{
    Metadata = new Dictionary<string, string>
    {
        { "payment_method_type", "cashapp" }
    }
};
```

**Customer-based (Future Orders):**
```csharp
// Saved to Stripe Customer
var customer = await customerService.CreateAsync(new CustomerCreateOptions
{
    Email = order.Email,
    Name = $"{order.FirstName} {order.LastName}",
    PaymentMethod = paymentMethodId,
    InvoiceSettings = new CustomerInvoiceSettingsOptions
    {
        DefaultPaymentMethod = paymentMethodId
    }
});
```

### Troubleshooting Payment Selection

**Issue: Payment method not showing**
```
Solution:
1. Enable in Stripe Dashboard (Settings ? Payment methods)
2. Check browser compatibility (e.g., Apple Pay needs Safari/iPhone)
3. Verify country/currency support
```

**Issue: Selection not persisting**
```
Solution: 
1. Check session storage
2. Verify Payment Intent is created
3. Review browser console for errors
```

**Issue: CashApp redirect fails**
```
Solution:
1. Verify return_url includes order ID
2. Check redirect_url format
3. Ensure HTTPS in production

Fixed in latest code:
return_url = $"{baseUrl}/Checkout/Complete?orderId={order.Id}"
```

---

## ?? Two-Step Checkout Process

### Overview

Your checkout uses a **two-step process** for better UX and security:

```
Step 1: Review Order        Step 2: Payment
???????????????????        ????????????????????
? ? Cart items    ?   ?    ? ? Payment method ?
? ? Quantities    ?        ? ? Billing info   ?
? ? Prices        ?        ? ? Card details   ?
? ? Shipping      ?        ? ? Submit payment ?
? ? Tax           ?        ?                  ?
???????????????????        ????????????????????
```

### Step 1: Review (/Checkout/Index)

**What happens:**
1. Display cart items with quantities and prices
2. Calculate subtotal, tax, shipping, total
3. Show customer information form
4. "Proceed to Payment" button

**Code flow:**
```csharp
public async Task<IActionResult> OnGetAsync()
{
    // Get cart items
    var cart = await _cartService.GetCartAsync();
    
    // Calculate totals
    Subtotal = cart.Sum(i => i.Quantity * i.Product.Price);
    Tax = Subtotal * 0.0825m; // 8.25%
    ShippingCost = 5.99m;
    Total = Subtotal + Tax + ShippingCost;
    
    return Page();
}
```

### Step 2: Payment (/Checkout/Payment)

**What happens:**
1. Create Stripe Payment Intent (server-side)
2. Load Stripe Elements (client-side)
3. Customer enters payment details
4. Submit and confirm payment
5. Redirect to confirmation page

**Code flow:**
```csharp
public async Task<IActionResult> OnGetAsync()
{
    // Create Payment Intent
    var intent = await _paymentService.CreatePaymentIntentAsync(
        amount: Total,
        currency: "usd",
        metadata: new Dictionary<string, string>
        {
            { "order_id", OrderId.ToString() },
            { "customer_email", Email }
        }
    );
    
    ClientSecret = intent.ClientSecret;
    return Page();
}
```

**Frontend (JavaScript):**
```javascript
// Initialize Stripe
const stripe = Stripe(publishableKey);
const elements = stripe.elements({ clientSecret });

// Create payment element
const paymentElement = elements.create('payment');
paymentElement.mount('#payment-element');

// Handle form submission
form.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const { error } = await stripe.confirmPayment({
        elements,
        confirmParams: {
            return_url: `${window.location.origin}/Checkout/Complete?orderId=${orderId}`
        }
    });
    
    if (error) {
        showError(error.message);
    }
});
```

### Benefits of Two-Step Process

? **Better UX:**
- Customer reviews before committing
- Can edit cart before payment
- Clear separation of concerns

? **Security:**
- Payment Intent created only when ready
- Reduces failed payment attempts
- Better fraud prevention

? **Conversion:**
- Reduces cart abandonment
- Clear progress indication
- Easy to go back and edit

---

## ?? Stripe Customer Integration

### Why Create Stripe Customers?

? **Benefits:**
- Faster checkout for returning customers
- Save payment methods securely
- View customer history in Stripe Dashboard
- Enable subscriptions (future feature)
- Better fraud detection
- Detailed customer insights

### Customer Creation Flow

```
Order Placed ? Check if Customer exists ? Create/Update Customer ? Attach Payment ? Complete Order
```

**Implementation:**
```csharp
public async Task<Customer> CreateOrUpdateCustomerAsync(Order order, string paymentMethodId)
{
    var customers = await _customerService.ListAsync(new CustomerListOptions
    {
        Email = order.Email,
        Limit = 1
    });
    
    Customer customer;
    
    if (customers.Data.Count > 0)
    {
        // Update existing customer
        customer = await _customerService.UpdateAsync(
            customers.Data[0].Id,
            new CustomerUpdateOptions
            {
                Name = $"{order.FirstName} {order.LastName}",
                Phone = order.PhoneNumber,
                Address = new AddressOptions
                {
                    Line1 = order.ShippingAddress,
                    City = order.City,
                    State = order.State,
                    PostalCode = order.ZipCode,
                    Country = "US"
                },
                PaymentMethod = paymentMethodId,
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            });
    }
    else
    {
        // Create new customer
        customer = await _customerService.CreateAsync(
            new CustomerCreateOptions
            {
                Email = order.Email,
                Name = $"{order.FirstName} {order.LastName}",
                Phone = order.PhoneNumber,
                Address = new AddressOptions
                {
                    Line1 = order.ShippingAddress,
                    City = order.City,
                    State = order.State,
                    PostalCode = order.ZipCode,
                    Country = "US"
                },
                PaymentMethod = paymentMethodId,
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            });
    }
    
    return customer;
}
```

### Viewing Customers in Stripe

```
Stripe Dashboard ? Customers ? Search by email/name
```

**Customer Details Show:**
- Name, email, phone
- Payment methods saved
- Order history
- Lifetime value
- Location

### Customer Benefits

**For returning customers:**
1. Payment methods pre-filled
2. Shipping address saved
3. Faster checkout
4. Better fraud protection

**For business:**
1. Customer lifetime value tracking
2. Repeat customer identification
3. Better analytics
4. Marketing opportunities

---

## ?? Google Pay & Apple Pay

### Google Pay

**Requirements:**
- Chrome browser or Android device
- Google account with saved cards
- HTTPS website (required in production)

**How it works:**
1. Customer clicks "Google Pay" tab
2. Google Pay modal appears
3. Customer selects card from Google account
4. Payment processed instantly

**Configuration:**
```javascript
// Automatically enabled via Stripe Elements
const paymentElement = elements.create('payment', {
  // Google Pay shows automatically if:
  // 1. Browser supports it (Chrome/Android)
  // 2. User has Google Pay set up
  // 3. Merchant ID is verified (production)
});
```

**Testing Google Pay:**
```
1. Use Chrome browser
2. Have a Google account signed in
3. Add test card to Google Pay:
   - Card: 4242 4242 4242 4242
   - Expiry: Any future date
   - CVC: Any 3 digits
```

### Apple Pay

**Requirements:**
- Safari browser, iPhone, iPad, or Mac
- Apple ID with saved cards
- Touch ID or Face ID enabled
- HTTPS website (required)

**How it works:**
1. Customer clicks "Apple Pay" button
2. Apple Pay modal appears
3. Customer authenticates (Touch ID/Face ID)
4. Payment processed instantly

**Configuration:**
```javascript
// Automatically enabled via Stripe Elements
const paymentElement = elements.create('payment', {
  // Apple Pay shows automatically if:
  // 1. Apple device or Safari browser
  // 2. Apple Pay is set up
  // 3. Domain verification (production)
});
```

**Domain Verification (Production):**
```
1. Stripe Dashboard ? Settings ? Apple Pay
2. Add your domain
3. Download verification file
4. Upload to: https://yourdomain.com/.well-known/apple-developer-merchantid-domain-association
```

**Testing Apple Pay:**
```
1. Use Safari or Apple device
2. Set up Apple Pay with test card:
   - Card: 4242 4242 4242 4242
   - Expiry: Any future date
   - CVC: Any 3 digits
```

### Browser Compatibility

| Payment Method | Chrome | Safari | Firefox | Edge | Mobile |
|----------------|--------|--------|---------|------|--------|
| Card | ? | ? | ? | ? | ? |
| CashApp | ? | ? | ? | ? | ? |
| Google Pay | ? | ? | ? | ? | ? (Android) |
| Apple Pay | ? | ? | ? | ? | ? (iOS) |
| Link | ? | ? | ? | ? | ? |

---

## ?? Troubleshooting

### Common Issues

#### 1. "Invalid API Key" Error

**Symptoms:**
```
Stripe.Exception.AuthenticationException: Invalid API Key provided
```

**Causes:**
- Wrong key format
- Test key in production mode
- Key not configured

**Solutions:**
```powershell
# Verify key is set
dotnet user-secrets list

# Check key format
# Test key should start with: sk_test_
# Live key should start with: sk_live_

# Re-add key
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"
```

#### 2. Payment Element Not Loading

**Symptoms:**
- Blank payment form
- Console error: "Stripe is not defined"

**Solutions:**
```html
<!-- Verify Stripe.js is loaded BEFORE your scripts -->
<script src="https://js.stripe.com/v3/"></script>
<script src="~/js/checkout.js"></script>

<!-- Check publishable key is passed -->
<input type="hidden" id="stripe-publishable-key" value="@Model.StripePublishableKey" />
```

#### 3. CashApp Redirect Loop

**Symptoms:**
- After CashApp payment, page keeps redirecting
- Order not completing

**Solution:**
```csharp
// FIXED: Ensure return_url includes order ID
var options = new PaymentIntentCreateOptions
{
    // ...
    PaymentMethodOptions = new PaymentIntentPaymentMethodOptionsOptions
    {
        CashappPay = new PaymentIntentPaymentMethodOptionsCashappOptions
        {
            CaptureMethod = "automatic",
            SetupFutureUsage = "off_session"
        }
    }
};

// On payment page
confirmParams: {
    return_url: `${window.location.origin}/Checkout/Complete?orderId=${orderId}`
}
```

#### 4. "Payment Intent Already Confirmed"

**Symptoms:**
- Error when submitting payment multiple times

**Solution:**
```csharp
// Check payment intent status before creating new one
var intent = await _paymentIntentService.GetAsync(existingIntentId);

if (intent.Status == "succeeded")
{
    // Payment already completed, redirect to success
    return RedirectToPage("/Checkout/Complete", new { orderId = order.Id });
}
```

#### 5. Webhook Not Receiving Events

**Symptoms:**
- Payments successful but orders not updating

**Solutions:**
```powershell
# 1. Verify webhook endpoint is registered
Stripe Dashboard ? Developers ? Webhooks ? Add endpoint
URL: https://yourdomain.com/api/webhook/stripe

# 2. Check webhook secret
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_YOUR_SECRET"

# 3. Test webhook locally with Stripe CLI
stripe listen --forward-to localhost:5001/api/webhook/stripe
stripe trigger payment_intent.succeeded
```

#### 6. "Application Restart Required"

**Symptoms:**
- Configuration changes not taking effect
- Still seeing old key values

**Solution:**
```powershell
# Stop application
Ctrl+C

# Clear any cached configurations
dotnet clean

# Rebuild and run
dotnet build
dotnet run
```

---

## ?? Quick Reference

### Configuration Hierarchy
```
1. User Secrets (Development) ? Highest priority
2. Environment Variables
3. appsettings.{Environment}.json
4. appsettings.json ? Lowest priority
```

### Important URLs
```
Stripe Dashboard:     https://dashboard.stripe.com
Test Data:            https://stripe.com/docs/testing
API Reference:        https://stripe.com/docs/api
Webhooks:             https://dashboard.stripe.com/webhooks
Customer Portal:      https://dashboard.stripe.com/customers
```

### Test Cards
```
Success:           4242 4242 4242 4242
Decline:           4000 0000 0000 0002
Auth Required:     4000 0025 0000 3155
Insufficient:      4000 0000 0000 9995
```

### Common Commands
```powershell
# User Secrets
dotnet user-secrets list
dotnet user-secrets set "Stripe:SecretKey" "sk_test_KEY"
dotnet user-secrets remove "Stripe:SecretKey"

# Test Webhook Locally
stripe listen --forward-to localhost:5001/api/webhook/stripe
stripe trigger payment_intent.succeeded

# View Stripe Logs
stripe logs tail
```

### Key Files
```
Configuration:
- appsettings.json (template)
- appsettings.Development.json (template)
- User secrets (secure storage)

Payment Processing:
- Services/StripePaymentService.cs
- Pages/Checkout/Payment.cshtml.cs
- wwwroot/js/checkout.js

Models:
- Models/StripeSettings.cs
- Models/Order.cs
```

### Status Flow
```
Payment Intent Statuses:
requires_payment_method ? requires_confirmation ? processing ? succeeded ? canceled
```

---

## ? Checklist

### Development Setup
- [ ] Stripe account created
- [ ] Test API keys obtained
- [ ] User secrets configured
- [ ] Test payment successful
- [ ] Webhooks tested locally
- [ ] All payment methods enabled

### Production Deployment
- [ ] Live API keys obtained
- [ ] Keys stored in Azure Key Vault
- [ ] Webhook endpoint registered
- [ ] Domain verified (Apple Pay)
- [ ] SSL certificate installed
- [ ] Payment methods tested in production
- [ ] Error handling tested
- [ ] Logging configured

### Security Checklist
- [ ] No keys in source code
- [ ] appsettings.Production.json in .gitignore
- [ ] Different keys for dev/prod
- [ ] Webhook signature verification enabled
- [ ] HTTPS enforced
- [ ] Key rotation schedule established

---

## ?? Additional Resources

- [Stripe Documentation](https://stripe.com/docs)
- [Stripe .NET SDK](https://github.com/stripe/stripe-dotnet)
- [Payment Element Guide](https://stripe.com/docs/payments/payment-element)
- [Testing Guide](https://stripe.com/docs/testing)
- [Security Best Practices](https://stripe.com/docs/security)

---

**Need help?** Check the troubleshooting section or contact Stripe support at https://support.stripe.com

**Everything consolidated from:**
- STRIPE_SETUP.md
- STRIPE_INTEGRATION_SUMMARY.md
- STRIPE_KEYS_QUICK_SETUP.md
- HOW_TO_ACCESS_STRIPE_KEYS.md
- SECURE_STRIPE_KEY_MANAGEMENT_GUIDE.md
- STRIPE_CUSTOMER_INTEGRATION_GUIDE.md
- STRIPE_ERROR_FIX_RESTART_REQUIRED.md
- MULTIPLE_PAYMENT_METHODS_GUIDE.md
- PAYMENT_METHODS_QUICK_REF.md
- PAYMENT_METHOD_SELECTION_GUIDE.md
- PAYMENT_METHOD_SELECTION_FIXES.md
- PAYMENT_METHOD_PERSISTENCE_FIX.md
- PAYMENT_METHOD_SPECIFIC_FIX.md
- CASHAPP_REDIRECT_FIX.md
- GOOGLE_APPLE_PAY_EXPLAINED.md
- CHECKOUT_QUICK_REFERENCE.md
- TWO_STEP_CHECKOUT_SUMMARY.md
