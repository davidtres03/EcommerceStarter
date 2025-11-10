using System.Security.Cryptography;
using System.Text;

namespace EcommerceStarter.Services
{
    public class OrderNumberService : IOrderNumberService
    {
        private const string Prefix = "CAP-";
        private const int CodeLength = 8; // Length of the random part
        private const string AllowedChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed confusing chars like 0, O, I, 1

        /// <summary>
        /// Generates a random order number with format: CAP-XXXXXXXX
        /// </summary>
        public string GenerateOrderNumber()
        {
            var random = new StringBuilder(CodeLength);
            
            using (var rng = RandomNumberGenerator.Create())
            {
                var buffer = new byte[CodeLength];
                rng.GetBytes(buffer);
                
                for (int i = 0; i < CodeLength; i++)
                {
                    var index = buffer[i] % AllowedChars.Length;
                    random.Append(AllowedChars[index]);
                }
            }
            
            return $"{Prefix}{random}";
        }

        /// <summary>
        /// Generates a unique order number by checking against existing orders
        /// </summary>
        /// <param name="existsCheck">Function to check if order number already exists</param>
        /// <returns>Unique order number</returns>
        public async Task<string> GenerateUniqueOrderNumberAsync(Func<string, Task<bool>> existsCheck)
        {
            const int maxAttempts = 10;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var orderNumber = GenerateOrderNumber();
                
                if (!await existsCheck(orderNumber))
                {
                    return orderNumber;
                }
            }
            
            // Fallback: add timestamp if we somehow get 10 collisions (extremely unlikely)
            var timestamp = DateTime.UtcNow.Ticks.ToString().Substring(0, 6);
            return $"{Prefix}{timestamp}{GenerateOrderNumber().Substring(Prefix.Length, 2)}";
        }
    }
}
