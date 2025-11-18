# ?? Email Configuration Guide - SMTP

**Complete guide to configuring SMTP email service for your EcommerceStarter store**

---

## ?? Table of Contents

1. [What is SMTP?](#what-is-smtp)
2. [SMTP vs Resend](#smtp-vs-resend)
3. [Popular SMTP Providers](#popular-smtp-providers)
4. [Gmail Configuration](#gmail-configuration)
5. [Outlook/Microsoft 365 Configuration](#outlookmicrosoft-365-configuration)
6. [SendGrid Configuration](#sendgrid-configuration)
7. [Mailgun Configuration](#mailgun-configuration)
8. [Custom SMTP Server](#custom-smtp-server)
9. [Configuring in the Installer](#configuring-in-the-installer)
10. [Testing Your Setup](#testing-your-setup)
11. [Troubleshooting](#troubleshooting)

---

## ?? What is SMTP?

**SMTP** (Simple Mail Transfer Protocol) is the standard protocol for sending emails. It's been around since 1982 and is universally supported.

**SMTP allows your store to send:**
- ?? Order confirmations
- ?? Shipping notifications
- ?? Password reset emails
- ?? Payment receipts
- ?? Customer notifications

---

## ?? SMTP vs Resend

| Feature | SMTP | Resend |
|---------|------|--------|
| **Setup Complexity** | Medium (15-30 min) | Easy (5 min) |
| **Cost** | Often free (with limits) | Free tier (100/day) |
| **Deliverability** | Variable (can be marked spam) | Excellent |
| **Rate Limits** | Provider-dependent | 100/day free |
| **Analytics** | Limited/None | Built-in dashboard |
| **Security** | App passwords/OAuth required | API key (modern) |
| **Best For** | Existing email infrastructure | New setups, developers |

**?? Recommendation:** If you're starting fresh, consider [Resend](EMAIL_RESEND_SETUP.md) for easier setup and better deliverability.

---

## ?? Popular SMTP Providers

### Free Options

| Provider | Free Limit | Pros | Cons |
|----------|-----------|------|------|
| **Gmail** | 500/day | Easy setup, reliable | Strict limits, not for high volume |
| **Outlook** | 300/day | Good for small stores | Lower limit |
| **SendGrid** | 100/day | Professional features | Requires verification |

### Paid Options

| Provider | Starting Price | Best For |
|----------|---------------|----------|
| **SendGrid** | $15/month (40k emails) | Growing businesses |
| **Mailgun** | $35/month (50k emails) | Developers |
| **Amazon SES** | $0.10/1000 emails | High volume, AWS users |
| **Postmark** | $15/month (10k emails) | Transactional emails |

---

## ?? Gmail Configuration

### Prerequisites

- Gmail account (personal or Google Workspace)
- 2-Step Verification enabled
- App Password generated

### Step 1: Enable 2-Step Verification

1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Under **"Signing in to Google"**, click **"2-Step Verification"**
3. Follow the prompts to set it up (phone verification)

### Step 2: Generate App Password

1. Go to [App Passwords](https://myaccount.google.com/apppasswords)
2. Select **"Mail"** from the app dropdown
3. Select **"Other (Custom name)"** from device dropdown
4. Enter name: **"EcommerceStarter"**
5. Click **"Generate"**
6. **Copy the 16-character password** (e.g., `abcd efgh ijkl mnop`)
7. **Remove spaces** when using: `abcdefghijklmnop`

### SMTP Settings

```
Host: smtp.gmail.com
Port: 587 (TLS) or 465 (SSL)
Username: your.email@gmail.com
Password: [16-character app password]
Encryption: TLS (Port 587) or SSL (Port 465)
```

### Configuration Example

**Installer Settings:**
- **SMTP Host:** `smtp.gmail.com`
- **SMTP Port:** `587`
- **Username:** `yourstore@gmail.com`
- **Password:** `abcdefghijklmnop` (app password, no spaces)

### Important Notes

?? **Daily Limit:** 500 emails per day  
?? **Use App Password:** Never use your regular Gmail password  
?? **From Address:** Will be your Gmail address  

**?? Tip:** Use a dedicated Gmail account for your store (e.g., `orders@yourdomain.com` via Google Workspace).

---

## ?? Outlook/Microsoft 365 Configuration

### Prerequisites

- Outlook.com or Microsoft 365 account
- Account security configured

### Step 1: Enable SMTP

SMTP is enabled by default for Outlook/Microsoft 365 accounts.

### Step 2: (Optional) Generate App Password

For better security:

1. Go to [Microsoft Account Security](https://account.microsoft.com/security)
2. Click **"Advanced security options"**
3. Under **"App passwords"**, click **"Create a new app password"**
4. Name it: **"EcommerceStarter"**
5. Copy the generated password

### SMTP Settings

**For Outlook.com (personal):**
```
Host: smtp-mail.outlook.com
Port: 587 (TLS)
Username: your.email@outlook.com
Password: [your password or app password]
Encryption: TLS (STARTTLS)
```

**For Microsoft 365 (business):**
```
Host: smtp.office365.com
Port: 587 (TLS)
Username: your.email@yourdomain.com
Password: [your password or app password]
Encryption: TLS (STARTTLS)
```

### Configuration Example

**Installer Settings:**
- **SMTP Host:** `smtp-mail.outlook.com` (or `smtp.office365.com`)
- **SMTP Port:** `587`
- **Username:** `yourstore@outlook.com`
- **Password:** `your-password` (or app password)

### Important Notes

?? **Daily Limit:** 300 emails per day (personal), varies for business  
?? **From Address:** Must match your Outlook/Microsoft 365 email  

---

## ?? SendGrid Configuration

SendGrid is a professional email service designed for transactional emails.

### Step 1: Create SendGrid Account

1. Go to [https://sendgrid.com](https://sendgrid.com)
2. Sign up for a **free account** (100 emails/day)
3. Verify your email address
4. Complete sender verification

### Step 2: Create API Key

1. Log in to SendGrid Dashboard
2. Go to **Settings** ? **API Keys**
3. Click **"Create API Key"**
4. Choose **"Restricted Access"**
5. Grant **"Mail Send"** permission
6. Click **"Create & View"**
7. **Copy the API key** (starts with `SG.`)

### Step 3: Verify Sender Identity

**Option A: Single Sender Verification (Quick)**
1. Go to **Settings** ? **Sender Authentication**
2. Click **"Verify a Single Sender"**
3. Enter your email (e.g., `orders@yourstore.com`)
4. Verify via email

**Option B: Domain Authentication (Recommended)**
1. Go to **Settings** ? **Sender Authentication**
2. Click **"Authenticate Your Domain"**
3. Enter your domain
4. Add DNS records shown
5. Wait for verification

### SMTP Settings

```
Host: smtp.sendgrid.net
Port: 587 (TLS) or 465 (SSL)
Username: apikey (literally "apikey")
Password: [Your SendGrid API key]
Encryption: TLS (Port 587) or SSL (Port 465)
```

### Configuration Example

**Installer Settings:**
- **SMTP Host:** `smtp.sendgrid.net`
- **SMTP Port:** `587`
- **Username:** `apikey` (yes, exactly this)
- **Password:** `SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

### Important Notes

?? **Username is "apikey":** Don't use your email, use the literal word "apikey"  
?? **Free Tier:** 100 emails/day  
? **Excellent Deliverability:** Professional service  
? **Analytics:** Built-in dashboard  

---

## ?? Mailgun Configuration

Mailgun is a developer-friendly email service with powerful APIs.

### Step 1: Create Mailgun Account

1. Go to [https://www.mailgun.com](https://www.mailgun.com)
2. Sign up (free tier: 100 emails/day for 3 months, then paid)
3. Verify your email and account

### Step 2: Get SMTP Credentials

1. Log in to Mailgun Dashboard
2. Go to **Sending** ? **Domain settings**
3. Find **SMTP credentials** section
4. Note the:
   - SMTP hostname
   - Port
   - Username
   - Password (click "Reset password" if needed)

### Step 3: Verify Domain (Recommended)

1. Add your domain in Dashboard
2. Add DNS records (SPF, DKIM, MX)
3. Verify

### SMTP Settings

```
Host: smtp.mailgun.org
Port: 587 (TLS) or 465 (SSL)  
Username: postmaster@your-sandbox.mailgun.org
Password: [Your Mailgun password]
Encryption: TLS (Port 587) or SSL (Port 465)
```

### Configuration Example

**Installer Settings:**
- **SMTP Host:** `smtp.mailgun.org`
- **SMTP Port:** `587`
- **Username:** `postmaster@sandboxXXXX.mailgun.org`
- **Password:** `your-mailgun-password`

### Important Notes

?? **Sandbox Mode:** Free accounts start in sandbox (limited recipients)  
?? **Domain Verification:** Required for production  
? **Developer-Friendly:** Great API, logs, and analytics  

---

## ?? Custom SMTP Server

If you have your own email server or web host's SMTP:

### Common Web Hosting SMTP Settings

**cPanel/WHM:**
```
Host: mail.yourdomain.com (or server hostname)
Port: 587 (TLS) or 465 (SSL)
Username: your-email@yourdomain.com
Password: your-email-password
```

**Plesk:**
```
Host: smtp.yourdomain.com
Port: 587 or 465
Username: your-email@yourdomain.com
Password: your-email-password
```

### Getting Your SMTP Settings

Contact your hosting provider or check:
- cPanel ? Email Accounts ? Configure Mail Client
- Hosting control panel documentation
- "Setup Guide" or "Email Configuration" section

---

## ?? Configuring in the Installer

### During Installation

1. When you reach the **Configuration** page
2. Check ?? **"Configure Email Notifications"**
3. Select **"SMTP"** from the dropdown
4. Enter your SMTP settings:
   - **SMTP Host:** (e.g., `smtp.gmail.com`)
   - **SMTP Port:** (usually `587`)
   - **Username:** Your email address
   - **Password:** Your password or app password
5. Click **"?? Test SMTP"** to validate

### What the Validator Checks

The installer will:
- ? Verify host and port are valid
- ? Test connection to SMTP server
- ? Attempt authentication
- ? Check for common issues (firewall, SSL)

### Success Message

```
? SMTP connection successful!

Host: smtp.gmail.com
Port: 587
SSL: True

Your email configuration is ready!
```

---

## ?? Testing Your Setup

### After Installation

1. **Complete the installation**
2. **Place a test order** on your store
3. **Check your email** for order confirmation
4. **Verify spam folder** if not received

### Manual Test (Advanced)

From your Admin Dashboard:
1. Go to **Settings** ? **Email**
2. Click **"Send Test Email"**
3. Enter recipient email
4. Check inbox

---

## ?? Troubleshooting

### "SMTP server not available"

**Causes:**
- Incorrect hostname
- Server is down
- Firewall blocking connection

**Solutions:**
1. Verify hostname spelling
2. Check [provider status page](#getting-help)
3. Try from a different network
4. Contact hosting provider

### "Authentication failed"

**Causes:**
- Wrong username or password
- Not using app password (Gmail, Outlook)
- 2FA enabled but no app password

**Solutions:**
1. **Gmail/Outlook:** Use app password, not regular password
2. Double-check credentials
3. Copy-paste to avoid typos
4. Ensure no extra spaces

### "Connection timed out"

**Causes:**
- Firewall blocking port
- ISP blocking SMTP
- Wrong port number

**Solutions:**
1. Try different port:
   - Port 587 (TLS/STARTTLS)
   - Port 465 (SSL)
   - Port 25 (not recommended)
2. Check firewall settings
3. Contact hosting provider
4. Try from different server

### Emails Going to Spam

**Causes:**
- SPF/DKIM records missing
- Sending from free email (Gmail, Yahoo)
- New domain/low reputation
- Spammy content

**Solutions:**
1. **Use professional email provider** (SendGrid, Mailgun)
2. **Set up SPF record:**
   ```
   v=spf1 include:_spf.google.com ~all  (for Gmail)
   v=spf1 include:sendgrid.net ~all     (for SendGrid)
   ```
3. **Add DKIM record** (from provider dashboard)
4. **Warm up your domain** (start with low volume)
5. **Avoid spam triggers:**
   - Don't use ALL CAPS
   - No excessive exclamation marks!!!
   - Include unsubscribe link
   - Use professional formatting

### Rate Limit Exceeded

**Causes:**
- Exceeded daily limit (Gmail: 500, Outlook: 300)
- Sent too many emails too quickly

**Solutions:**
1. Wait for limit reset (usually 24 hours)
2. Upgrade to paid plan
3. Switch to professional service (SendGrid, Mailgun)
4. Spread sends over time

### SSL Certificate Errors

**Causes:**
- Server certificate invalid
- Certificate expired
- Self-signed certificate

**Solutions:**
1. Ensure using correct host (e.g., `smtp.gmail.com` not `gmail.com`)
2. Use port 587 (TLS) instead of 465 (SSL)
3. Contact provider if persistent
4. Update .NET runtime if very old

---

## ?? Security Best Practices

### Protecting Credentials

? **DO:**
- Use app passwords (Gmail, Outlook)
- Store in secure configuration (installer handles this)
- Use dedicated email account for store
- Enable 2FA on email account

? **DON'T:**
- Use your personal email password
- Commit credentials to repositories
- Share SMTP credentials
- Reuse passwords

### Monitoring

1. **Review email logs** regularly
2. **Monitor failed sends**
3. **Check for unauthorized access** to email account
4. **Rotate passwords** periodically

---

## ? Quick Reference

### Common SMTP Ports

| Port | Protocol | Use Case |
|------|----------|----------|
| **587** | TLS (STARTTLS) | **Recommended** (most compatible) |
| **465** | SSL | Legacy, still supported |
| **25** | Plain/TLS | Often blocked by ISPs |
| **2525** | Alternative | Backup if 587 is blocked |

### Provider Quick Settings

**Gmail:**
```
Host: smtp.gmail.com
Port: 587
User: your@gmail.com
Pass: [app password]
```

**Outlook:**
```
Host: smtp-mail.outlook.com
Port: 587
User: your@outlook.com
Pass: [your password]
```

**SendGrid:**
```
Host: smtp.sendgrid.net
Port: 587
User: apikey
Pass: SG.xxxxx
```

**Mailgun:**
```
Host: smtp.mailgun.org
Port: 587
User: postmaster@xxx.mailgun.org
Pass: [mailgun password]
```

---

## ?? Getting Help

### Provider Support

- **Gmail:** [https://support.google.com/mail](https://support.google.com/mail)
- **Outlook:** [https://support.microsoft.com/outlook](https://support.microsoft.com/outlook)
- **SendGrid:** [https://support.sendgrid.com](https://support.sendgrid.com)
- **Mailgun:** [https://help.mailgun.com](https://help.mailgun.com)

### EcommerceStarter Support

- **Documentation:** `docs/` folder
- **GitHub Issues:** Report bugs
- **Community:** GitHub Discussions

---

**Prefer easier setup?** Consider [Resend](EMAIL_RESEND_SETUP.md) for modern API-based email.

**Ready to configure?** Continue with the installer and use your SMTP settings!

---

*Last updated: January 2025*  
*EcommerceStarter v1.0*
