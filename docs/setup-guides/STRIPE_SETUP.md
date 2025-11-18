# ?? Stripe Payment Integration Setup Guide

**Complete guide to configuring Stripe payments for your EcommerceStarter store**

---

## ?? Table of Contents

1. [What is Stripe?](#what-is-stripe)
2. [Creating a Stripe Account](#creating-a-stripe-account)
3. [Getting Your API Keys](#getting-your-api-keys)
4. [Test vs Live Mode](#test-vs-live-mode)
5. [Configuring in the Installer](#configuring-in-the-installer)
6. [Testing Your Integration](#testing-your-integration)
7. [Going Live](#going-live)
8. [Troubleshooting](#troubleshooting)

---

## ?? What is Stripe?

**Stripe** is a payment processing platform that allows your store to accept:
- ?? Credit and debit cards (Visa, Mastercard, Amex, Discover)
- ?? Digital wallets (Apple Pay, Google Pay, Cash App Pay)
- ?? Link by Stripe (one-click checkout)

**Benefits:**
- ? Industry-leading security (PCI-compliant)
- ? No monthly fees (only pay per transaction)
- ? Easy integration
- ? Built-in fraud protection
- ? Works in 135+ countries

---

## ?? Creating a Stripe Account

### Step 1: Sign Up

1. Go to [https://stripe.com](https://stripe.com)
2. Click **"Sign Up"** or **"Start now"**
3. Enter your email address and create a password
4. Verify your email address

### Step 2: Complete Your Profile

Stripe will ask for:
- Business name
- Business type (individual, company, non-profit)
- Business address
- Industry/products you sell
- Website URL (optional during testing)

**?? Tip:** You can start with minimal information and complete your profile later before going live.

---

## ?? Getting Your API Keys

### Where to Find Your Keys

1. **Log in** to your Stripe Dashboard: [https://dashboard.stripe.com](https://dashboard.stripe.com)
2. Click **"Developers"** in the top-right menu
3. Click **"API keys"** in the left sidebar

You'll see two types of keys:

### Publishable Key
```
pk_test_EXAMPLE_REDACTED
```
- **Purpose:** Used on your website's frontend (visible to customers)
- **Safe to share:** Can be included in your HTML/JavaScript
- **Starts with:** `pk_test_` (test mode) or `pk_live_` (live mode)

### Secret Key
```
sk_test_EXAMPLE_KEY_REDACTED
```
- **Purpose:** Used on your server (never shown to customers)
- **KEEP SECURE:** Never share this key or commit it to public repositories
- **Starts with:** `sk_test_` (test mode) or `sk_live_` (live mode)

### Copying Your Keys

1. Click the **"Reveal test key"** button for the Secret key
2. Click the **copy icon** (??) next to each key
3. Save both keys somewhere safe (you'll need them for installation)

**?? Important:** Keep your secret key secure! Anyone with your secret key can process payments from your account.

---

## ?? Test vs Live Mode

Stripe has two separate modes:

### Test Mode ??

**When to use:**
- During development and testing
- Before your store goes live
- Testing checkout flow
- Training staff

**Test Keys:**
- Publishable: `pk_test_...`
- Secret: `sk_test_...`

**Test Credit Cards:**
Stripe provides test card numbers that you can use:

| Card Number | Brand | Result |
|-------------|-------|--------|
| 4242 4242 4242 4242 | Visa | Success |
| 4000 0000 0000 9995 | Visa | Declined |
| 4000 0025 0000 3155 | Visa | Requires authentication |

**Use any:**
- Future expiration date (e.g., 12/34)
- Any 3-digit CVC
- Any billing ZIP code

**?? No real charges:** Test mode never processes real payments!

### Live Mode ??

**When to use:**
- After testing is complete
- When you're ready to accept real payments
- Your store is publicly available

**Live Keys:**
- Publishable: `pk_live_...`
- Secret: `sk_live_...`

**?? Warning:** Live mode processes REAL payments and charges REAL money!

---

## ?? Configuring in the Installer

### During Installation

1. When you reach the **Configuration** page
2. Check ?? **"Configure Stripe Payment Processing"**
3. Enter your **Publishable Key**
4. Enter your **Secret Key**
5. Click **"? Validate Keys"** to test them

### What the Validator Checks

The installer will:
- ? Verify key format (starts with `pk_` and `sk_`)
- ? Check that both keys are test or both are live (no mixing)
- ? Test the publishable key by creating a token
- ? Test the secret key by connecting to Stripe API
- ? Confirm both keys are active and valid

### Success Message

You should see:
```
? Both Stripe keys validated successfully!

Mode: TEST MODE
Publishable key: pk_test_YOUR_KEY_HERE
Secret key: sk_test_YOUR_KEY_HERE

? You are using test keys. Remember to switch 
to live keys before going to production!
```

---

## ?? Testing Your Integration

### After Installation

1. **Visit your store** (http://yourdomain.com)
2. **Add a product** to your cart
3. **Go to checkout**
4. **Use a test card:**
   - Card: `4242 4242 4242 4242`
   - Expiry: `12/34`
   - CVC: `123`
   - ZIP: `12345`
5. **Complete the order**

### Verify in Stripe Dashboard

1. Go to [Stripe Dashboard](https://dashboard.stripe.com)
2. Make sure you're in **Test Mode** (toggle in top-right)
3. Click **"Payments"** in left sidebar
4. You should see your test payment listed!

---

## ?? Going Live

### Before Switching to Live Mode

**Complete your Stripe account:**
1. Log in to [Stripe Dashboard](https://dashboard.stripe.com)
2. Click **"Activate account"** banner at the top
3. Provide required business information:
   - Legal business name
   - Tax ID (EIN, SSN, or equivalent)
   - Bank account for payouts
   - Business address
   - Website URL
   - Products/services description

**Enable payment methods:**
1. Go to **Settings** ? **Payment methods**
2. Enable the methods you want:
   - Credit/debit cards ? (enabled by default)
   - Apple Pay ?
   - Google Pay ?
   - Link by Stripe ?

**Review settings:**
1. **Payouts:** Set your payout schedule (daily, weekly, monthly)
2. **Receipts:** Configure email receipts for customers
3. **Statements:** Set up bank statement descriptors

### Switching to Live Keys

**Option A: During Initial Installation**
1. Get your **live keys** from Stripe Dashboard
   - Switch to **Live mode** in top-right
   - Go to **Developers** ? **API keys**
2. Enter **live keys** during installer configuration
3. Validate them before proceeding

**Option B: After Installation (Reconfigure)**
1. Run the installer again (it will detect existing installation)
2. Update your Stripe keys to **live keys**
3. The installer will update your configuration

**Option C: Manual Update**
1. Open your **Admin Dashboard** (http://yourdomain.com/Admin)
2. Go to **Settings** ? **Integrations**
3. Update Stripe keys
4. Save changes

### Final Checks

? **Test live mode with a real card:**
- Use your own credit card
- Make a small purchase ($1)
- Verify it appears in Stripe Dashboard (Live mode)
- Issue a refund

? **Check email notifications:**
- Verify customers receive order confirmations
- Check Stripe receipts are sent

? **Test the full flow:**
- Add to cart
- Checkout
- Payment
- Order confirmation
- Order appears in Admin panel

---

## ?? Troubleshooting

### Common Issues

#### "Invalid publishable key"

**Causes:**
- Key is incomplete (missing characters)
- Key is from wrong mode (test vs live)
- Key has been revoked/regenerated

**Solutions:**
1. Copy the **complete key** from Stripe Dashboard
2. Ensure you're copying from the **correct mode**
3. Check for extra spaces before/after the key
4. Regenerate the key if needed

#### "Invalid secret key"

**Causes:**
- Key is incorrect or has been revoked
- Wrong mode (mixing test and live)

**Solutions:**
1. Click **"Reveal test key"** in Stripe Dashboard
2. Copy the **complete key** (all ~107 characters)
3. Ensure mode matches publishable key
4. Don't share secret key (it may have been compromised)

#### "Keys must both be test keys or both be live keys"

**Cause:** You're mixing test and live keys

**Solution:**
- Both keys must be from the **same mode**
- Check the prefix: `pk_test_` with `sk_test_` OR `pk_live_` with `sk_live_`

#### Payments not appearing in dashboard

**Causes:**
- Wrong mode selected in dashboard
- Using test keys but looking at live mode

**Solutions:**
1. Toggle between **Test** and **Live** mode in Stripe Dashboard
2. Verify which keys you're using in your store configuration

#### "Your card was declined"

**Causes (Test Mode):**
- Using a real card in test mode
- Using a declined test card intentionally

**Solutions:**
- Use Stripe test cards: `4242 4242 4242 4242`
- Don't use real cards in test mode

**Causes (Live Mode):**
- Insufficient funds
- Bank declined the card
- Incorrect card details

**Solutions:**
- Ask customer to try a different card
- Verify card details are correct
- Check Stripe Dashboard for decline reason

---

## ?? Getting Help

### Stripe Resources

- **Dashboard:** [https://dashboard.stripe.com](https://dashboard.stripe.com)
- **Documentation:** [https://stripe.com/docs](https://stripe.com/docs)
- **Support:** [https://support.stripe.com](https://support.stripe.com)
- **Status Page:** [https://status.stripe.com](https://status.stripe.com)

### EcommerceStarter Support

- **Documentation:** `docs/` folder in your installation
- **GitHub Issues:** Report bugs and request features
- **Community:** GitHub Discussions

---

## ?? Security Best Practices

### Protecting Your Secret Key

? **DO:**
- Store in secure environment variables
- Use the installer's secure configuration
- Restrict access to admin panel
- Regenerate if compromised

? **DON'T:**
- Commit to public repositories
- Share in emails or chat
- Include in client-side code
- Hard-code in your application

### Monitoring for Fraud

Stripe provides built-in fraud protection, but you should:

1. **Enable Radar:** Stripe's fraud detection (included free)
2. **Review disputes:** Check for chargebacks regularly
3. **Set up alerts:** Get notified of suspicious activity
4. **Use 3D Secure:** Enable for high-risk transactions

---

## ? Quick Reference

### Key Format

```
Publishable:  pk_test_YOUR_KEY_HERE  (Test)
             pk_live_YOUR_KEY_HERE  (Live)

Secret:      sk_test_YOUR_KEY_HERE  (Test)
             sk_live_YOUR_KEY_HERE  (Live)
```

### Test Cards

```
Success:     4242 4242 4242 4242
Declined:    4000 0000 0000 9995
Auth Required: 4000 0025 0000 3155

Expiry: Any future date
CVC: Any 3 digits
ZIP: Any postal code
```

### Stripe Fees

- **Standard rate:** 2.9% + $0.30 per transaction
- **No setup fees**
- **No monthly fees**
- **No hidden costs**

---

**Need more help?** Check the [Stripe Documentation](https://stripe.com/docs) or contact [Stripe Support](https://support.stripe.com).

**Ready to configure?** Continue with the installer and use your API keys!

---

*Last updated: January 2025*  
*EcommerceStarter v1.0*

