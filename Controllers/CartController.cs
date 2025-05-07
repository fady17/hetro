// File: Hetro/Controllers/CartController.cs
using Hetro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Hetro.Controllers
{
    [Authorize] // All cart actions require login
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync();
            return View(cart); // Pass cart to view
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (quantity < 1) quantity = 1; // Ensure at least 1
            await _cartService.AddItemAsync(productId, quantity);
            _logger.LogInformation("Product {ProductId} (Qty: {Quantity}) added to cart.", productId, quantity);
            TempData["StatusMessage"] = "Item added to cart!";
            return RedirectToAction("Index", "Home"); // Or redirect to cart, or back to product
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            await _cartService.RemoveItemAsync(cartItemId);
            _logger.LogInformation("Cart item {CartItemId} removed from cart.", cartItemId);
            TempData["StatusMessage"] = "Item removed from cart.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync();
             _logger.LogInformation("Cart cleared.");
            TempData["StatusMessage"] = "Cart cleared.";
            return RedirectToAction("Index");
        }
    }
}