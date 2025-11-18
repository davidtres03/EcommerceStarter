# ?? Email Configuration Guide - Resend

**Complete guide to configuring Resend email service for your EcommerceStarter store**

---

## ?? Table of Contents

1. [What is Resend?](#what-is-resend)
2. [Why Choose Resend?](#why-choose-resend)
3. [Creating a Resend Account](#creating-a-resend-account)
4. [Getting Your API Key](#getting-your-api-key)
5. [Verifying Your Domain](#verifying-your-domain)
6. [Configuring in the Installer](#configuring-in-the-installer)
7. [Testing Your Setup](#testing-your-setup)
8. [Email Templates](#email-templates)
9. [Troubleshooting](#troubleshooting)

---

## ?? What is Resend?

**Resend** is a modern email API designed for developers. It allows your store to send:
- ?? Order confirmations
- ?? Shipping notifications
- ?? Password reset emails
- ?? Payment receipts
- ?? Customer notifications

**Key Features:**
- ? Simple, modern API
- ? Excellent deliverability
- ? Built-in analytics
- ? Generous free tier (100 emails/day)
- ? Developer-friendly
- ? Beautiful email templates

---

## ? Why Choose Resend?

### vs Traditional SMTP

| Feature | Resend | SMTP (Gmail, etc.) |
|---------|--------|-------------------|
| **Setup Time** | 5 minutes | 15-30 minutes |
| **Deliverability** | Excellent | Can be flagged as spam |
| **Rate Limits** | 100/day free | 500/day (Gmail) |
| **Analytics** | Built-in dashboard | None |
| **Templates** | Supported | Manual |
| **Domain Verification** | Easy (DNS records) | App passwords required |
| **Security** | Modern API | Legacy protocols |

### Free Tier

Resend offers a generous free tier:
- **100 emails per day** (3,000/month)
- Perfect for small to medium stores
- No credit card required to start
- Upgrade when you grow

---

## ?? Creating a Resend Account

### Step 1: Sign Up

1. Go to [https://resend.com](https://resend.com)
2. Click **"Get Started"** or **"Sign Up"**
3. Enter your email address
4. Verify your email (check inbox/spam)
5. Complete your profile

### Step 2: Create Your First API Key

1. After signing in, you'll see the dashboard
2. Click **"API Keys"** in the left sidebar
3. Click **"Create API Key"**
4. Give it a name (e.g., "EcommerceStarter Production")
5. Choose **Permission**:
   - **Sending access** (Recommended - more secure)
   - **Full access** (For advanced usage)
6. Click **"Add"**
7. **Copy the API key** immediately (you won't see it again!)

**?? Tip:** Save your API key in a secure location. You'll need it for the installer.

---

## ?? Getting Your API Key

### API Key Format

Resend API keys look like this:
```
re_123456789abcdefghijklmnopqrstuvwxyz
```

- **Starts with:** `re_`
- **Length:** ~36 characters
- **Type:** Alphanumeric

### Key Types

#### Sending Access (Recommended) ??
- **Can:** Send emails
- **Cannot:** Read emails, manage domains, delete keys
- **Best for:** Production use (more secure)
- **What you'll see:** Restricted key warnings are normal and expected!

#### Full Access
- **Can:** Everything (send, read, manage)
- **Use case:** Development, testing, full control
- **Security:** Less secure if compromised

**? Recommendation:** Use **Sending Access** keys for your store. They're more secure!

---

## ?? Verifying Your Domain

### Why Verify Your Domain?

**Benefits:**
- ? Higher deliverability (emails won't go to spam)
- ? Send from your domain (e.g., orders@yourstore.com)
- ? Builds trust with customers
- ? Professional appearance

**Without verification:**
- ?? Emails sent from onboarding@resend.dev
- ?? Lower deliverability
- ?? Looks less professional

### Verification Steps

#### Step 1: Add Your Domain

1. Go to [Resend Dashboard](https://resend.com/domains)
2. Click **"Add Domain"**
3. Enter your domain (e.g., `yourstore.com`)
4. Click **"Add"**

#### Step 2: Add DNS Records

Resend will show you DNS records to add:

**SPF Record:**
```
Type: TXT
Name: @
Value: v=spf1 include:resend.com ~all
```

**DKIM Record:**
```
Type: TXT
Name: resend._domainkey
Value: [provided by Resend]
```

**DMARC Record (Optional but recommended):**
```
Type: TXT
Name: _dmarc
Value: v=DMARC1; p=none; rua=mailto:dmarc@yourstore.com
```

#### Step 3: Add Records to Your DNS Provider

**Common providers:**

**Cloudflare:**
1. Log in to Cloudflare
2. Select your domain
3. Go to **DNS** ? **Records**
4. Click **"Add record"**
5. Add each record from Resend
6. **Proxy status:** Set to **DNS only** (gray cloud)

**GoDaddy:**
1. Log in to GoDaddy
2. Go to **My Products** ? **DNS**
3. Click **"Add"** for each record
4. Enter values from Resend

**Namecheap:**
1. Log in to Namecheap
2. Select your domain ? **Advanced DNS**
3. Click **"Add New Record"**
4. Add each record from Resend

#### Step 4: Verify

1. Return to Resend Dashboard
2. Click **"Verify"** next to your domain
3. Wait for verification (usually instant, can take up to 24 hours)
4. ? Status will change to **"Verified"**

**?? Tip:** DNS changes can take up to 48 hours to propagate, but usually complete within minutes.

---

## ?? Configuring in the Installer

### During Installation

1. When you reach the **Configuration** page
2. Check ?? **"Configure Email Notifications"**
3. Select **"Resend (Recommended)"** from the dropdown
4. Enter your **Resend API Key**
5. Click **"? Test API"** to validate

### What the Validator Checks

The installer will:
- ? Verify key format (starts with `re_`)
- ? Test connection to Resend API
- ? Confirm key is active and valid
- ? Check key permissions

### Success Messages

**For Sending Access Key (Recommended):**
```
? Resend API key validated successfully!

Note: This is a RESTRICTED key that can only send emails.
This is perfect for production use and more secure!
```

**For Full Access Key:**
```
? Resend API key validated successfully!

Your API key has full permissions and can send emails.
```

---

## ?? Testing Your Setup

### After Installation

1. **Complete the installation**
2. **Place a test order** on your store
3. **Check your email** for order confirmation
4. **Verify in Resend Dashboard:**
   - Go to [Resend Dashboard](https://resend.com/emails)
   - You should see the sent email listed
   - Click it to view details

### Test Email Content

Your store will send various emails:

**Order Confirmation:**
- ?? **To:** Customer email
- ?? **Subject:** "Order Confirmation #12345"
- ?? **Contains:** Order details, items, total, payment info

**Shipping Notification:**
- ?? **To:** Customer email
- ?? **Subject:** "Your order has shipped!"
- ?? **Contains:** Tracking number, carrier, estimated delivery

**Password Reset:**
- ?? **To:** User email
- ?? **Subject:** "Reset your password"
- ?? **Contains:** Reset link, expiration time

---

## ?? Email Templates

EcommerceStarter includes pre-built email templates that work with Resend:

### Customizing Templates

1. **Via Admin Panel:**
   - Log in to Admin Dashboard
   - Go to **Settings** ? **Email Templates**
   - Customize subject, body, footer
   - Use variables: `{{customerName}}`, `{{orderNumber}}`, etc.

2. **Via Code (Advanced):**
   - Templates are in `Views/Emails/`
   - Written in Razor syntax
   - Fully customizable HTML

### Template Variables

Available in all templates:
- `{{storeName}}` - Your store name
- `{{storeUrl}}` - Your store URL
- `{{customerName}}` - Customer's name
- `{{customerEmail}}` - Customer's email
- `{{orderNumber}}` - Order number
- `{{orderTotal}}` - Order total
- `{{orderDate}}` - Order date

---

## ?? Troubleshooting

### "Authentication failed with Resend"

**Causes:**
- API key is incorrect
- Key has been revoked
- Extra spaces in the key

**Solutions:**
1. Go to [Resend Dashboard](https://resend.com/api-keys)
2. Create a **new API key**
3. Copy the **complete key** (all 36 characters)
4. Ensure no extra spaces before/after
5. Try again

### "This API key is restricted to only send emails"

**This is GOOD!** ?

This message means:
- Your key is a **Sending Access** key (recommended)
- It's **more secure** than full access keys
- It **can send emails** (which is what you need)
- The installer recognizes this as **valid**

**Action:** Click "Next" to continue!

### Emails Going to Spam

**Causes:**
- Domain not verified
- SPF/DKIM records missing
- Low sender reputation (new domain)

**Solutions:**
1. **Verify your domain** (see above)
2. **Add all DNS records:**
   - SPF ?
   - DKIM ?
   - DMARC ? (recommended)
3. **Warm up your domain:**
   - Send small volumes initially
   - Gradually increase
   - Avoid spam trigger words
4. **Check Resend Analytics:**
   - Go to Dashboard ? Emails
   - Look for delivery issues
   - Review bounce rates

### Emails Not Sending

**Check these:**

1. **Resend Dashboard:**
   - Go to [Emails](https://resend.com/emails)
   - Look for failed sends
   - Check error messages

2. **Rate Limits:**
   - Free tier: 100/day
   - Check your usage
   - Upgrade if needed

3. **API Key Status:**
   - Ensure key is active
   - Not expired or revoked

4. **Store Configuration:**
   - Admin Dashboard ? Settings
   - Verify Resend is selected
   - API key is correct

### "Invalid email address"

**Causes:**
- Customer email is malformed
- Domain doesn't exist

**Solutions:**
- Validate email addresses on checkout
- Use proper email regex validation
- Test with a valid email

---

## ?? Monitoring & Analytics

### Resend Dashboard

Access at: [https://resend.com/emails](https://resend.com/emails)

**Metrics available:**
- ?? **Emails sent** - Total count
- ? **Delivered** - Successfully delivered
- ?? **Bounced** - Failed deliveries
- ??? **Opened** - Email opens (if tracking enabled)
- ??? **Clicked** - Link clicks

### Email Logs

Each email shows:
- **Timestamp** - When sent
- **Recipient** - To whom
- **Subject** - Email subject
- **Status** - Delivered, bounced, pending
- **Events** - Timeline of delivery

### Webhooks (Advanced)

Set up webhooks to get real-time notifications:
- Email delivered
- Email bounced
- Email opened
- Link clicked

**Setup:**
1. Resend Dashboard ? **Webhooks**
2. Add endpoint URL
3. Select events
4. Configure in your store

---

## ?? Pricing

### Free Tier
- **100 emails/day** (3,000/month)
- All features included
- No credit card required
- Perfect for small stores

### Paid Plans

**Pro Plan** - $20/month:
- **50,000 emails/month**
- Additional emails: $1/1,000
- Priority support
- Advanced analytics

**Enterprise:**
- Custom volume
- Dedicated IP
- SLA guarantees
- Custom pricing

**Check current pricing:** [https://resend.com/pricing](https://resend.com/pricing)

---

## ?? Security Best Practices

### Protecting Your API Key

? **DO:**
- Use **Sending Access** keys (restricted permissions)
- Store securely (installer handles this)
- Rotate keys periodically
- Delete unused keys

? **DON'T:**
- Commit to public repositories
- Share in emails or chat
- Use full access unless needed
- Hard-code in your application

### Monitoring Usage

1. **Set up alerts:**
   - Unusual sending volume
   - High bounce rates
   - API key usage

2. **Review logs regularly:**
   - Check for unauthorized sends
   - Monitor delivery rates
   - Review bounces

---

## ? Quick Reference

### API Key Format
```
re_123456789abcdefghijklmnopqrstuvwxyz
```

### DNS Records (Example)
```
SPF:
Type: TXT
Name: @
Value: v=spf1 include:resend.com ~all

DKIM:
Type: TXT  
Name: resend._domainkey
Value: [from Resend dashboard]

DMARC:
Type: TXT
Name: _dmarc
Value: v=DMARC1; p=none
```

### Common Email Types
- Order confirmation ?
- Shipping notification ??
- Password reset ??
- Welcome email ??
- Receipt ??

---

## ?? Getting Help

### Resend Resources
- **Dashboard:** [https://resend.com](https://resend.com)
- **Documentation:** [https://resend.com/docs](https://resend.com/docs)
- **Support:** [support@resend.com](mailto:support@resend.com)
- **Status:** [https://status.resend.com](https://status.resend.com)

### EcommerceStarter Support
- **Documentation:** `docs/` folder
- **GitHub Issues:** Report bugs
- **Community:** GitHub Discussions

---

**Need SMTP instead?** See [EMAIL_SMTP_SETUP.md](EMAIL_SMTP_SETUP.md)

**Ready to configure?** Continue with the installer and use your API key!

---

*Last updated: January 2025*  
*EcommerceStarter v1.0*
