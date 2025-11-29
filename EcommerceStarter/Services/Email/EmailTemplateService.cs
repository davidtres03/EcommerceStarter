using EcommerceStarter.Models;
using Microsoft.AspNetCore.Http;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Generates branded HTML email templates for all providers
    /// Templates are provider-agnostic and include site branding
    /// </summary>
    public class EmailTemplateService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStoredImageService _storedImageService;

        public EmailTemplateService(IHttpContextAccessor httpContextAccessor, IStoredImageService storedImageService)
        {
            _httpContextAccessor = httpContextAccessor;
            _storedImageService = storedImageService;
        }

        public async Task<string> GenerateOrderConfirmation(Order order, SiteSettings settings)
        {
            var logoUrl = await GetEmailLogoUrlAsync(settings);
            var itemsHtml = string.Join("", order.OrderItems.Select(item => $@"
                <tr>
                    <td style=""padding: 12px; border-bottom: 1px solid #e5e7eb;"">{item.Product?.Name ?? "Product"}</td>
                    <td style=""padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: center;"">{item.Quantity}</td>
                    <td style=""padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right;"">${item.UnitPrice:F2}</td>
                    <td style=""padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right; font-weight: 600;"">${item.TotalPrice:F2}</td>
                </tr>
            "));

            var supportEmail = string.IsNullOrEmpty(settings.SupportEmail) ? settings.ContactEmail : settings.SupportEmail;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Order Confirmation</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 40px 0; text-align: center;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, {settings.EmailHeaderColor}, {settings.PrimaryDark}); padding: 32px; text-align: center; border-radius: 12px 12px 0 0;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $"<img src='{logoUrl}' alt='{settings.SiteName}' style='max-height: 60px; margin-bottom: 16px;'>")}
                            <h1 style=""color: white; margin: 0; font-size: 28px;"">Welcome!</h1>
                            <p style=""color: rgba(255, 255, 255, 0.9); margin: 8px 0 0 0; font-size: 16px;"">Thank you for your order</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px;"">
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Hi {order.ShippingName},</p>
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">We've received your order and will send you a shipping confirmation email as soon as your order ships.</p>
                            
                            <div style=""background: #f9fafb; border-radius: 8px; padding: 20px; margin-bottom: 24px;"">
                                <h2 style=""margin: 0 0 16px 0; font-size: 18px; color: #111827;"">Order Details</h2>
                                <p style=""margin: 0 0 8px 0; color: #6b7280;""><strong>Order Number:</strong> {order.OrderNumber}</p>
                                <p style=""margin: 0 0 8px 0; color: #6b7280;""><strong>Order Date:</strong> {order.OrderDate.ToLocalTime():MMMM dd, yyyy}</p>
                                <p style=""margin: 0; color: #6b7280;""><strong>Payment Status:</strong> <span style=""color: #059669; font-weight: 600;"">Paid</span></p>
                            </div>
                            
                            <table style=""width: 100%; border-collapse: collapse; margin-bottom: 24px;"">
                                <thead>
                                    <tr style=""background: #f9fafb;"">
                                        <th style=""padding: 12px; text-align: left; font-size: 14px; color: #6b7280; font-weight: 600;"">Product</th>
                                        <th style=""padding: 12px; text-align: center; font-size: 14px; color: #6b7280; font-weight: 600;"">Qty</th>
                                        <th style=""padding: 12px; text-align: right; font-size: 14px; color: #6b7280; font-weight: 600;"">Price</th>
                                        <th style=""padding: 12px; text-align: right; font-size: 14px; color: #6b7280; font-weight: 600;"">Total</th>
                                    </tr>
                                </thead>
                                <tbody>{itemsHtml}</tbody>
                            </table>
                            
                            <div style=""border-top: 2px solid #e5e7eb; padding-top: 16px;"">
                                <table style=""width: 100%; margin-bottom: 8px;"">
                                    <tr>
                                        <td style=""text-align: right; padding: 4px 0; color: #6b7280;"">Subtotal:</td>
                                        <td style=""text-align: right; padding: 4px 0; width: 100px; font-weight: 600;"">${order.Subtotal:F2}</td>
                                    </tr>
                                    <tr>
                                        <td style=""text-align: right; padding: 4px 0; color: #6b7280;"">Tax:</td>
                                        <td style=""text-align: right; padding: 4px 0; width: 100px; font-weight: 600;"">${order.TaxAmount:F2}</td>
                                    </tr>
                                    <tr>
                                        <td style=""text-align: right; padding: 4px 0; color: #6b7280; font-size: 18px; font-weight: 600;"">Total:</td>
                                        <td style=""text-align: right; padding: 4px 0; width: 100px; font-size: 20px; font-weight: 700; color: {settings.PrimaryColor};"">${order.TotalAmount:F2}</td>
                                    </tr>
                                </table>
                            </div>
                            
                            <div style=""background: #f9fafb; border-radius: 8px; padding: 20px; margin-top: 24px;"">
                                <h3 style=""margin: 0 0 12px 0; font-size: 16px; color: #111827;"">Shipping Address</h3>
                                <p style=""margin: 0; color: #6b7280; line-height: 1.6;"">
                                    {order.ShippingName}<br>{order.ShippingAddress}<br>{order.ShippingCity}, {order.ShippingState} {order.ShippingZip}
                                </p>
                            </div>
                            
                            <p style=""font-size: 14px; color: #6b7280; margin: 24px 0 0 0; text-align: center;"">
                                Questions? Contact us at <a href=""mailto:{supportEmail}"" style=""color: {settings.PrimaryColor}; text-decoration: none;"">{supportEmail}</a>
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: #f9fafb; padding: 24px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0 0 12px 0; font-size: 14px; color: #6b7280;"">{settings.EmailFooterText}</p>
                            <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">&copy; {DateTime.Now.Year} {settings.CompanyName}. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public async Task<string> GenerateShippingNotification(Order order, string trackingNumber, SiteSettings settings)
        {
            var logoUrl = await GetEmailLogoUrlAsync(settings);
            
            // Generate tracking link if we have a courier
            string trackingLinkHtml = "";
            if (!string.IsNullOrEmpty(trackingNumber) && order.TrackingCourier != Courier.Unknown)
            {
                var trackingUrl = order.TrackingCourier.GenerateTrackingUrl(trackingNumber);
                if (!string.IsNullOrEmpty(trackingUrl))
                {
                    trackingLinkHtml = $@"
                            <div style=""text-align: center; margin: 24px 0;"">
                                <a href=""{trackingUrl}"" style=""display: inline-block; background: linear-gradient(135deg, {settings.EmailButtonColor}, {settings.PrimaryDark}); color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px;"">Track Your Package</a>
                            </div>";
                }
            }

            var trackingHtml = string.IsNullOrEmpty(trackingNumber) 
                ? "" 
                : $@"<p style=""margin: 0 0 8px 0; color: #065f46;""><strong>Tracking Number:</strong> <code style=""background: #f0fdf4; padding: 4px 8px; border-radius: 4px;"">{trackingNumber}</code></p>
                    <p style=""margin: 0; color: #065f46; font-size: 14px;""><strong>Carrier:</strong> {order.TrackingCourier.GetShortName()}</p>";

            var supportEmail = string.IsNullOrEmpty(settings.SupportEmail) ? settings.ContactEmail : settings.SupportEmail;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Order Shipped</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 40px 0; text-align: center;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, {settings.EmailHeaderColor}, {settings.PrimaryDark}); padding: 32px; text-align: center; border-radius: 12px 12px 0 0;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $"<img src='{logoUrl}' alt='{settings.SiteName}' style='max-height: 60px; margin-bottom: 16px;'>")}
                            <h1 style=""color: white; margin: 0; font-size: 28px;"">Your Order Has Shipped!</h1>
                            <p style=""color: rgba(255, 255, 255, 0.9); margin: 8px 0 0 0; font-size: 16px;"">Order #{order.OrderNumber}</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px;"">
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Hi {order.ShippingName},</p>
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Great news! Your order has been shipped and is on its way to you.</p>
                            
                            <div style=""background: #f0fdf4; border-left: 4px solid #059669; border-radius: 8px; padding: 20px; margin-bottom: 24px;"">
                                <h2 style=""margin: 0 0 16px 0; font-size: 18px; color: #065f46;"">Shipping Information</h2>
                                <p style=""margin: 0 0 8px 0; color: #065f46;""><strong>Order Number:</strong> {order.OrderNumber}</p>
                                <p style=""margin: 0 0 8px 0; color: #065f46;""><strong>Ship Date:</strong> {DateTime.Now:MMMM dd, yyyy}</p>
                                {trackingHtml}
                            </div>
                            
                            {trackingLinkHtml}
                            
                            <div style=""background: #f9fafb; border-radius: 8px; padding: 20px; margin-bottom: 24px;"">
                                <h3 style=""margin: 0 0 12px 0; font-size: 16px; color: #111827;"">Shipping To:</h3>
                                <p style=""margin: 0; color: #6b7280; line-height: 1.6;"">
                                    {order.ShippingName}<br>{order.ShippingAddress}<br>{order.ShippingCity}, {order.ShippingState} {order.ShippingZip}
                                </p>
                            </div>
                            
                            <p style=""font-size: 14px; color: #6b7280; text-align: center;"">
                                Thank you for shopping with us! Questions? <a href=""mailto:{supportEmail}"" style=""color: {settings.PrimaryColor}; text-decoration: none;"">{supportEmail}</a>
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: #f9fafb; padding: 24px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0 0 12px 0; font-size: 14px; color: #6b7280;"">{settings.EmailFooterText}</p>
                            <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">&copy; {DateTime.Now.Year} {settings.CompanyName}. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public async Task<string> GenerateEmailVerification(ApplicationUser user, string verificationLink, SiteSettings settings)
        {
            var logoUrl = await GetEmailLogoUrlAsync(settings);
            var supportEmail = string.IsNullOrEmpty(settings.SupportEmail) ? settings.ContactEmail : settings.SupportEmail;

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verify Your Email</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 40px 0; text-align: center;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, {settings.EmailHeaderColor}, {settings.PrimaryDark}); padding: 32px; text-align: center; border-radius: 12px 12px 0 0;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $"<img src='{logoUrl}' alt='{settings.SiteName}' style='max-height: 60px; margin-bottom: 16px;'>")}
                            <h1 style=""color: white; margin: 0; font-size: 28px;"">Welcome to {settings.SiteName}!</h1>
                            <p style=""color: rgba(255, 255, 255, 0.9); margin: 8px 0 0 0; font-size: 16px;"">Verify your email to get started</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px;"">
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Hi {user.Email},</p>
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Thank you for creating an account with {settings.SiteName}! We're excited to have you on board.</p>
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">To complete your registration and start shopping, please verify your email address by clicking the button below:</p>
                            
                            <div style=""text-align: center; margin: 32px 0;"">
                                <a href=""{verificationLink}"" style=""display: inline-block; background: linear-gradient(135deg, {settings.EmailButtonColor}, {settings.PrimaryDark}); color: white; padding: 16px 32px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px;"">Verify Email Address</a>
                            </div>
                            
                            <div style=""background: #fef3c7; border-left: 4px solid #f59e0b; border-radius: 8px; padding: 16px; margin: 24px 0;"">
                                <p style=""margin: 0; color: #92400e; font-size: 14px;""><strong>Note:</strong> This link expires in 24 hours. If you didn't create this account, you can safely ignore this email.</p>
                            </div>
                            
                            <p style=""font-size: 14px; color: #6b7280; margin: 24px 0 0 0;"">If the button doesn't work, copy and paste this link into your browser:<br><a href=""{verificationLink}"" style=""color: {settings.PrimaryColor}; word-break: break-all;"">{verificationLink}</a></p>
                            

                            <p style=""font-size: 14px; color: #6b7280; margin: 24px 0 0 0; text-align: center;"">Questions? Contact us at <a href=""mailto:{supportEmail}"" style=""color: {settings.PrimaryColor}; text-decoration: none;"">{supportEmail}</a></p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: #f9fafb; padding: 24px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0 0 12px 0; font-size: 14px; color: #6b7280;"">{settings.EmailFooterText}</p>
                            <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">&copy; {DateTime.Now.Year} {settings.CompanyName}. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public async Task<string> GeneratePasswordReset(string resetUrl, SiteSettings settings)
        {
            var logoUrl = await GetEmailLogoUrlAsync(settings);
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 40px 0; text-align: center;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, {settings.EmailHeaderColor}, {settings.PrimaryDark}); padding: 32px; text-align: center; border-radius: 12px 12px 0 0;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $"<img src='{logoUrl}' alt='{settings.SiteName}' style='max-height: 60px; margin-bottom: 16px;'>")}
                            <h1 style=""color: white; margin: 0; font-size: 28px;"">Password Reset Request</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px;"">
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">You've requested to reset your password for your {settings.SiteName} account.</p>
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Click the button below to reset your password:</p>
                            
                            <div style=""text-align: center; margin: 32px 0;"">
                                <a href=""{resetUrl}"" style=""display: inline-block; background: linear-gradient(135deg, {settings.EmailButtonColor}, {settings.PrimaryDark}); color: white; padding: 16px 32px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px;"">Reset Password</a>
                            </div>
                            
                            <div style=""background: #fef3c7; border-left: 4px solid #f59e0b; border-radius: 8px; padding: 16px; margin: 24px 0;"">
                                <p style=""margin: 0; color: #92400e; font-size: 14px;""><strong>Security Note:</strong> This link expires in 24 hours. If you didn't request this, ignore this email.</p>
                            </div>
                            
                            <p style=""font-size: 14px; color: #6b7280; margin: 24px 0 0 0;"">If the button doesn't work, copy and paste this link:<br><a href=""{resetUrl}"" style=""color: {settings.PrimaryColor}; word-break: break-all;"">{resetUrl}</a></p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: #f9fafb; padding: 24px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">&copy; {DateTime.Now.Year} {settings.CompanyName}. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public async Task<string> GenerateWelcomeEmail(ApplicationUser user, SiteSettings settings)
        {
            var logoUrl = await GetEmailLogoUrlAsync(settings);
            var supportEmail = string.IsNullOrEmpty(settings.SupportEmail) ? settings.ContactEmail : settings.SupportEmail;
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome</title>
</head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 40px 0; text-align: center;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, {settings.EmailHeaderColor}, {settings.PrimaryDark}); padding: 32px; text-align: center; border-radius: 12px 12px 0 0;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $"<img src=\"{logoUrl}\" alt=\"{settings.SiteName}\" style=\"max-height: 60px; margin-bottom: 16px;\">")}
                            <h1 style=""color: white; margin: 0; font-size: 28px;"">Welcome to {settings.SiteName}!</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px;"">
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Hi {user.Email},</p>
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Thank you for creating an account! We're excited to have you as part of our community.</p>
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Start exploring our products and enjoy shopping with us.</p>
                            <p style=""font-size: 14px; color: #6b7280; margin: 24px 0 0 0; text-align: center;"">Need help? <a href=""mailto:{supportEmail}"" style=""color: {settings.PrimaryColor}; text-decoration: none;"">{supportEmail}</a></p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: #f9fafb; padding: 24px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0 0 12px 0; font-size: 14px; color: #6b7280;"">{settings.EmailFooterText}</p>
                            <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">&copy; {DateTime.Now.Year} {settings.CompanyName}. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        public async Task<string> GenerateAdminNotification(Order order, SiteSettings settings)
        {
            var itemsHtml = string.Join("", order.OrderItems.Select(item => 
                $@"<li style=""margin-bottom: 8px; color: #6b7280;"">{item.Product?.Name ?? "Product"} x {item.Quantity} - ${item.TotalPrice:F2}</li>"));

            return $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><title>New Order</title></head>
<body style=""margin: 0; padding: 20px; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"">
    <div style=""max-width: 600px; margin: 0 auto; background: white; border-radius: 8px; padding: 24px; box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);"">
        <h2 style=""color: #111827; margin: 0 0 16px 0;"">New Order Received</h2>
        <div style=""background: #f0fdf4; border-left: 4px solid #059669; padding: 16px; margin-bottom: 24px; border-radius: 4px;"">
            <p style=""margin: 0; color: #065f46; font-size: 16px;""><strong>Order #{order.OrderNumber}</strong> - ${order.TotalAmount:F2}</p>
        </div>
        <h3 style=""color: #374151; font-size: 16px; margin: 0 0 12px 0;"">Customer:</h3>
        <p style=""color: #6b7280; margin: 0 0 8px 0;""><strong>Name:</strong> {order.ShippingName}</p>
        <p style=""color: #6b7280; margin: 0 0 8px 0;""><strong>Email:</strong> {order.CustomerEmail}</p>
        <p style=""color: #6b7280; margin: 0 0 16px 0;""><strong>Date:</strong> {order.OrderDate.ToLocalTime():g}</p>
        <h3 style=""color: #374151; font-size: 16px; margin: 0 0 12px 0;"">Items:</h3>
        <ul style=""padding-left: 20px; margin: 0 0 16px 0;"">{itemsHtml}</ul>
        <p style=""color: #374151; font-size: 18px; font-weight: 600; margin: 0;"">Total: ${order.TotalAmount:F2}</p>
    </div>
</body>
</html>";
        }

        public async Task<string> GenerateTestEmail(SiteSettings settings)
        {
            var logoUrl = await GetEmailLogoUrlAsync(settings);
            
            return $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""><title>Test Email</title></head>
<body style=""margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f3f4f6;"">
    <table role=""presentation"" style=""width: 100%; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 40px 0; text-align: center;"">
                <table role=""presentation"" style=""max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, {settings.EmailHeaderColor}, {settings.PrimaryDark}); padding: 32px; text-align: center; border-radius: 12px 12px 0 0;"">
                            {(string.IsNullOrEmpty(logoUrl) ? "" : $"<img src='{logoUrl}' alt='{settings.SiteName}' style='max-height: 60px; margin-bottom: 16px;'>")}
                            <h1 style=""color: white; margin: 0; font-size: 28px;"">Email Test</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 32px;"">
                            <p style=""font-size: 16px; color: #374151; margin: 0 0 24px 0;"">Congratulations! Your email configuration is working correctly.</p>
                            <div style=""background: #f0fdf4; border-left: 4px solid #059669; border-radius: 8px; padding: 16px;"">
                                <p style=""margin: 0; color: #065f46;""><strong>Configuration:</strong><br>
                                From: {settings.EmailFromName} &lt;{settings.EmailFromAddress}&gt;<br>
                                Time: {DateTime.Now:f}</p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background: #f9fafb; padding: 24px; text-align: center; border-radius: 0 0 12px 12px; border-top: 1px solid #e5e7eb;"">
                            <p style=""margin: 0; font-size: 12px; color: #9ca3af;"">&copy; {DateTime.Now.Year} {settings.CompanyName}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        /// <summary>
        /// Converts relative URLs to absolute URLs for email clients
        /// Email clients can't load relative URLs - they need full https://domain.com/path URLs
        /// </summary>
        private string MakeAbsoluteUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return "";
            
            // If already absolute, return as-is
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return url;
            
            // Get the current request's base URL
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                // Fallback: can't determine base URL without HttpContext
                // This shouldn't happen in normal operation
                return url;
            }

            var request = context.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
            
            // Ensure URL starts with /
            if (!url.StartsWith("/"))
                url = "/" + url;
            
            return $"{baseUrl}{url}";
        }

        /// <summary>
        /// Gets the email logo URL - returns "cid:logo" for CID embedding, or external URL as fallback
        /// </summary>
        private async Task<string> GetEmailLogoUrlAsync(SiteSettings settings)
        {
            // Priority 1: EmailLogoImageId - use CID embedding for email clients
            if (settings.EmailLogoImageId.HasValue && settings.EmailLogoImageId.Value != Guid.Empty)
            {
                var image = await _storedImageService.GetImageAsync(settings.EmailLogoImageId.Value);
                if (image != null && image.StorageType == "local")
                    return "cid:logo"; // CID reference for embedded attachment
            }

            // Priority 2: LogoImageId - use CID embedding
            if (settings.LogoImageId.HasValue && settings.LogoImageId.Value != Guid.Empty)
            {
                var image = await _storedImageService.GetImageAsync(settings.LogoImageId.Value);
                if (image != null && image.StorageType == "local")
                    return "cid:logo"; // CID reference for embedded attachment
            }

            // Priority 3: EmailLogoUrl (fallback to external URL - must be publicly accessible)
            if (!string.IsNullOrEmpty(settings.EmailLogoUrl))
            {
                return MakeAbsoluteUrl(settings.EmailLogoUrl);
            }

            // Priority 4: LogoUrl (fallback to external URL)
            if (!string.IsNullOrEmpty(settings.LogoUrl))
            {
                return MakeAbsoluteUrl(settings.LogoUrl);
            }

            return "";
        }
    }
}
