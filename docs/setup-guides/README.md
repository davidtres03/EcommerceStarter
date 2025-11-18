# ?? EcommerceStarter Setup Guides

**Quick links to setup guides for configuring your e-commerce store**

---

## ?? Getting Started

Welcome to EcommerceStarter! These guides will help you configure your store's essential services.

### Installation Order

1. **Install the application** using the Windows installer
2. **Configure Stripe** for payment processing
3. **Configure Email** for customer notifications
4. **Test your setup** with a test order
5. **Go live!**

---

## ?? Available Guides

### ?? Payment Processing

#### [Stripe Payment Setup](STRIPE_SETUP.md)
Complete guide to integrating Stripe payments.

**Covers:**
- Creating a Stripe account
- Getting API keys (test and live)
- Configuring in the installer
- Testing payments
- Going live with real transactions
- Troubleshooting common issues

**Time to complete:** ~15 minutes  
**Difficulty:** ? Easy

---

### ?? Email Configuration

#### [Resend Email Setup](EMAIL_RESEND_SETUP.md) ? Recommended
Modern API-based email service - easiest to configure.

**Covers:**
- Creating a Resend account
- Getting your API key
- Domain verification
- Testing email delivery
- Understanding restricted vs full access keys

**Time to complete:** ~10 minutes  
**Difficulty:** ? Easy  
**Free tier:** 100 emails/day

---

#### [SMTP Email Setup](EMAIL_SMTP_SETUP.md)
Traditional SMTP configuration with popular providers.

**Covers:**
- Gmail SMTP setup
- Outlook/Microsoft 365 SMTP
- SendGrid professional SMTP
- Mailgun configuration
- Custom SMTP servers
- Troubleshooting delivery issues

**Time to complete:** ~20-30 minutes  
**Difficulty:** ?? Moderate

---

## ?? Which Email Provider Should I Choose?

### Choose **Resend** if:
- ? You want the easiest setup
- ? You prefer modern API over legacy SMTP
- ? You want built-in analytics
- ? 100 emails/day is enough (free tier)
- ? You value excellent deliverability

### Choose **SMTP** if:
- ? You already have Gmail/Outlook for business
- ? You have existing SMTP infrastructure
- ? You prefer traditional email protocols
- ? Your hosting provider includes SMTP
- ? You need more than 100 emails/day (Gmail: 500/day)

### Choose **SendGrid/Mailgun** if:
- ? You need professional email service
- ? You're sending high volume (1000s/day)
- ? You want advanced analytics and APIs
- ? You need dedicated IP addresses
- ? Maximum deliverability is critical

---

## ? Quick Help

### Stripe Issues

**Problem:** "Invalid API key"  
**Solution:** See [Stripe Setup - Troubleshooting](STRIPE_SETUP.md#troubleshooting)

**Problem:** Keys don't match (test vs live)  
**Solution:** Both keys must be from same mode

**Problem:** Payment declined  
**Solution:** Use test card `4242 4242 4242 4242` in test mode

---

### Email Issues

**Problem:** "Authentication failed"  
**Solution:** 
- **Gmail/Outlook:** Use app password, not regular password
- **Resend:** Copy complete API key (all 36 characters)
- **SMTP:** Check username/password

**Problem:** Emails going to spam  
**Solution:**
- Verify your domain (Resend)
- Add SPF/DKIM records
- Use professional email provider

**Problem:** "Restricted API key" error  
**Solution:** This is actually GOOD for Resend! It means your key is secure. Continue with installation.

---

## ??? During Installation

### Configuration Page

When you reach the Configuration page in the installer:

1. **Basic Settings:**
   - Store name ?
   - Admin email ?
   - Admin password ?
   - Database settings ?

2. **Payment Processing (Optional but Recommended):**
   - ?? Check "Configure Stripe"
   - Enter your API keys
   - Click "? Validate Keys"
   - See [Stripe Setup Guide](STRIPE_SETUP.md)

3. **Email Notifications (Optional but Recommended):**
   - ?? Check "Configure Email"
   - Choose provider (Resend or SMTP)
   - Enter credentials
   - Click "? Test API" or "?? Test SMTP"
   - See [Resend Guide](EMAIL_RESEND_SETUP.md) or [SMTP Guide](EMAIL_SMTP_SETUP.md)

---

## ?? Testing Your Setup

### After Installation

1. **Access your store:** http://yourdomain.com
2. **Login to admin:** http://yourdomain.com/Admin/Dashboard
3. **Add a test product** (or use sample data)
4. **Place a test order:**
   - Add to cart
   - Checkout
   - Use Stripe test card: `4242 4242 4242 4242`
5. **Verify:**
   - ? Order appears in admin panel
   - ? Payment in Stripe Dashboard (test mode)
   - ? Customer receives order confirmation email

---

## ?? Going Live

### Pre-Launch Checklist

- [ ] Stripe keys switched to LIVE mode
- [ ] Email domain verified (if using Resend)
- [ ] Test order completed successfully
- [ ] Admin account password changed
- [ ] Sample products removed
- [ ] Store branding customized
- [ ] Terms & Privacy policy added
- [ ] SSL certificate installed (HTTPS)

### Launch!

1. Switch Stripe to live keys (see [Stripe Guide](STRIPE_SETUP.md#going-live))
2. Announce to customers
3. Monitor first orders closely
4. Check Stripe and email dashboards

---

## ?? Best Practices

### Security

- ? Use strong admin password
- ? Enable 2FA on Stripe account
- ? Use restricted API keys when possible
- ? Keep API keys secure (never commit to git)
- ? Monitor for unusual activity

### Email Deliverability

- ? Verify your domain
- ? Add SPF, DKIM, DMARC records
- ? Start with low volume, scale up
- ? Monitor bounce rates
- ? Don't send spam

### Customer Experience

- ? Test the full checkout flow
- ? Verify all emails are sent
- ? Ensure mobile responsiveness
- ? Fast page load times
- ? Clear shipping/return policies

---

## ?? Getting Help

### Documentation

- **This guide:** Overview and quick reference
- **Stripe Setup:** [STRIPE_SETUP.md](STRIPE_SETUP.md)
- **Resend Email:** [EMAIL_RESEND_SETUP.md](EMAIL_RESEND_SETUP.md)
- **SMTP Email:** [EMAIL_SMTP_SETUP.md](EMAIL_SMTP_SETUP.md)

### Support

- **GitHub Issues:** Report bugs and request features
- **GitHub Discussions:** Ask questions, share tips
- **Documentation:** `/docs` folder in repository

### External Resources

- **Stripe:** [https://stripe.com/docs](https://stripe.com/docs)
- **Resend:** [https://resend.com/docs](https://resend.com/docs)
- **SendGrid:** [https://docs.sendgrid.com](https://docs.sendgrid.com)

---

## ?? Additional Documentation

Looking for more guides?

- **Admin Guide:** Managing your store
- **Configuration Guide:** Advanced settings
- **Deployment Guide:** Hosting and deployment
- **Security Guide:** Securing your store
- **Troubleshooting:** Common issues and solutions

---

**Ready to start?** Choose a guide above and let's get your store configured! ??

---

*Last updated: January 2025*  
*EcommerceStarter v1.0*
