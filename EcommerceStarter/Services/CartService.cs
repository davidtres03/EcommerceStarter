using EcommerceStarter.Models;
using System.Text.Json;

namespace EcommerceStarter.Services
{
    public interface ICartService
    {
        List<CartItem> GetCart();
        void AddToCart(CartItem item);
        void UpdateQuantity(int productId, int quantity);
        void RemoveFromCart(int productId);
        void ClearCart();
        int GetCartItemCount();
        decimal GetCartTotal();
    }

    public class CartService : ICartService
    {
        private const string CartSessionKey = "ShoppingCart";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public List<CartItem> GetCart()
        {
            if (Session == null) return new List<CartItem>();

            var cartJson = Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            
            // Filter out expired items (older than 7 days)
            var validItems = cart.Where(c => c.ExpiresAt > DateTime.UtcNow).ToList();
            
            // If any items were expired, save the cleaned cart
            if (validItems.Count != cart.Count)
            {
                SaveCart(validItems);
            }
            
            return validItems;
        }

        public void AddToCart(CartItem item)
        {
            if (Session == null) return;

            // Validate input to prevent negative quantities or prices
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), "Cart item cannot be null");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException("Quantity must be positive", nameof(item.Quantity));
            }

            if (item.Price < 0)
            {
                throw new ArgumentException("Price cannot be negative", nameof(item.Price));
            }

            if (string.IsNullOrWhiteSpace(item.ProductName))
            {
                throw new ArgumentException("Product name is required", nameof(item.ProductName));
            }

            if (item.ProductId <= 0)
            {
                throw new ArgumentException("Invalid product ID", nameof(item.ProductId));
            }

            // Set expiration dates for cart items (7 days)
            item.AddedAt = DateTime.UtcNow;
            item.ExpiresAt = DateTime.UtcNow.AddDays(7);

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == item.ProductId);

            if (existingItem != null)
            {
                var newQuantity = existingItem.Quantity + item.Quantity;
                
                // Validate total quantity doesn't exceed stock (if stock is set)
                if (existingItem.StockQuantity > 0 && newQuantity > existingItem.StockQuantity)
                {
                    newQuantity = existingItem.StockQuantity;
                }
                
                // Validate total quantity doesn't exceed reasonable maximum
                if (newQuantity > 999)
                {
                    newQuantity = 999;
                }
                
                existingItem.Quantity = newQuantity;
                // Update expiration when quantity is updated
                existingItem.ExpiresAt = DateTime.UtcNow.AddDays(7);
                // Update stock info in case it changed
                if (item.StockQuantity > 0)
                {
                    existingItem.StockQuantity = item.StockQuantity;
                }
            }
            else
            {
                // Validate single item quantity doesn't exceed stock
                if (item.StockQuantity > 0 && item.Quantity > item.StockQuantity)
                {
                    item.Quantity = item.StockQuantity;
                }
                
                cart.Add(item);
            }

            SaveCart(cart);
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            if (Session == null) return;

            // Validate product ID
            if (productId <= 0)
            {
                throw new ArgumentException("Invalid product ID", nameof(productId));
            }

            // Validate quantity range
            if (quantity < 0)
            {
                throw new ArgumentException("Quantity cannot be negative", nameof(quantity));
            }

            if (quantity > 999)
            {
                throw new ArgumentException("Maximum quantity (999) exceeded", nameof(quantity));
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    // Validate quantity doesn't exceed available stock
                    if (item.StockQuantity > 0 && quantity > item.StockQuantity)
                    {
                        throw new InvalidOperationException($"Cannot exceed available stock of {item.StockQuantity} units");
                    }
                    
                    item.Quantity = quantity;
                }
                SaveCart(cart);
            }
        }

        public void RemoveFromCart(int productId)
        {
            if (Session == null) return;

            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
        }

        public void ClearCart()
        {
            Session?.Remove(CartSessionKey);
        }

        public int GetCartItemCount()
        {
            return GetCart().Sum(c => c.Quantity);
        }

        public decimal GetCartTotal()
        {
            return GetCart().Sum(c => c.Subtotal);
        }

        private void SaveCart(List<CartItem> cart)
        {
            if (Session == null) return;

            var cartJson = JsonSerializer.Serialize(cart);
            Session.SetString(CartSessionKey, cartJson);
        }
    }
}
