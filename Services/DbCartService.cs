// File: Hetro/Services/DbCartService.cs
using Hetro.Data;
using Hetro.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hetro.Services
{
    public interface ICartService
    {
        Task AddItemAsync(int productId, int quantity);
        Task<ShoppingCart?> GetCartAsync();
        Task RemoveItemAsync(int cartItemId);
        Task ClearCartAsync();
        Task<int> GetCartItemCountAsync();
    }

    public class DbCartService : ICartService
    {
        private readonly HetroDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService; // To get product details
        private readonly ILogger<DbCartService> _logger;

        public DbCartService(HetroDbContext context, IHttpContextAccessor httpContextAccessor, IProductService productService, ILogger<DbCartService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
            _logger = logger;
        }

        private string? GetUserSubjectId()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                   _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
        }

        private async Task<ShoppingCart> GetOrCreateCartForCurrentUserAsync()
        {
            var userSubjectId = GetUserSubjectId();
            if (string.IsNullOrEmpty(userSubjectId))
            {
                _logger.LogWarning("Attempted to get/create cart for unauthenticated user.");
                throw new InvalidOperationException("User must be logged in to manage cart.");
            }

            var cart = await _context.ShoppingCarts
                             .Include(c => c.Items)
                             .FirstOrDefaultAsync(c => c.UserSubjectId == userSubjectId);

            if (cart == null)
            {
                _logger.LogInformation("Creating new cart for user {UserSubjectId}", userSubjectId);
                // Ensure LocalUser exists, or create a placeholder if UserSyncService hasn't run yet
                // For robust flow, UserSyncService should have created LocalUser on login
                var localUser = await _context.LocalUsers.FindAsync(userSubjectId);
                if (localUser == null) {
                    // This shouldn't happen if UserSyncService ran on login
                    _logger.LogError("LocalUser not found for SubjectId {UserSubjectId} when creating cart. This indicates a sync issue.", userSubjectId);
                    throw new InvalidOperationException("User profile not found. Please try logging in again.");
                }

                cart = new ShoppingCart { UserSubjectId = userSubjectId, LastUpdatedUtc = DateTime.UtcNow };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync(); // Save cart to get its ID
            }
            return cart;
        }

        public async Task AddItemAsync(int productId, int quantity)
        {
            if (quantity <= 0) return;

            var cart = await GetOrCreateCartForCurrentUserAsync();
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null) {
                 _logger.LogWarning("Attempted to add non-existent product {ProductId} to cart.", productId);
                 return; // Or throw
            }

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = quantity
                });
            }
            cart.LastUpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<ShoppingCart?> GetCartAsync()
        {
            var userSubjectId = GetUserSubjectId();
            if (string.IsNullOrEmpty(userSubjectId)) return null; // No cart for anonymous users

            return await _context.ShoppingCarts
                             .Include(c => c.Items)
                             .AsNoTracking()
                             .FirstOrDefaultAsync(c => c.UserSubjectId == userSubjectId);
        }
        public async Task<int> GetCartItemCountAsync()
        {
            var cart = await GetCartAsync();
            return cart?.Items.Sum(i => i.Quantity) ?? 0;
        }

        public async Task RemoveItemAsync(int cartItemId)
        {
            var cart = await GetOrCreateCartForCurrentUserAsync(); // Ensures we act on current user's cart
            var itemToRemove = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (itemToRemove != null)
            {
                _context.CartItems.Remove(itemToRemove); // Or cart.Items.Remove(itemToRemove); EF tracks it
                cart.LastUpdatedUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            } else {
                 _logger.LogWarning("Attempted to remove non-existent cart item ID {CartItemId} from cart for user {UserSubjectId}", cartItemId, cart.UserSubjectId);
            }
        }

        public async Task ClearCartAsync()
        {
            var cart = await GetOrCreateCartForCurrentUserAsync();
            if (cart.Items.Any())
            {
                _context.CartItems.RemoveRange(cart.Items); // Remove all items from context
                cart.Items.Clear(); // Clear collection on the cart object
                cart.LastUpdatedUtc = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}