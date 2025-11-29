namespace EcommerceStarter.Services
{
    public class CustomerInfo
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public AddressInfo? Address { get; set; }
    }

    public class AddressInfo
    {
        public string? Line1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; } = "US";
    }

    public interface IPaymentService
    {
        Task<string> CreatePaymentIntentAsync(
            decimal amount, 
            string currency = "usd", 
            Dictionary<string, string>? metadata = null,
            CustomerInfo? customerInfo = null);
        
        Task<bool> ConfirmPaymentAsync(string paymentIntentId);
        Task<string> GetPaymentStatusAsync(string paymentIntentId);
        Task<(bool success, string? refundId, string? error)> RefundPaymentAsync(string paymentIntentId, long amountInCents, string reason = "requested_by_customer");
    }
}
