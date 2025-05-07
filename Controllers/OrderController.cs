// File: Hetro/Controllers/OrderController.cs
using Hetro.Data;
using Hetro.Models;
using Hetro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hetro.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly HetroDbContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(HetroDbContext context, ICartService cartService, ILogger<OrderController> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        // GET: /Order/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var userSubjectId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userSubjectId))
            {
                return Challenge(); // Should not happen if [Authorize] is on controller
            }

            var orders = await _context.Orders
                                .Where(o => o.UserSubjectId == userSubjectId)
                                .Include(o => o.OrderItems) // Include items for display
                                .OrderByDescending(o => o.OrderDateUtc)
                                .ToListAsync();

            return View(orders); // Pass list of orders to the view
        }

        // GET: /Order/Checkout
        public async Task<IActionResult> Checkout()
    {
        var cart = await _cartService.GetCartAsync();
        if (cart == null || !cart.Items.Any())
        {
            _logger.LogWarning("Checkout attempt with empty cart.");
            TempData["ErrorMessage"] = "Your cart is empty. Please add items before checking out.";
            return RedirectToAction("Index", "Cart");
        }

        var userSubjectId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        // --- ADDED: Check if UserSubjectId was actually found ---
        if (string.IsNullOrEmpty(userSubjectId))
        {
            _logger.LogError("UserSubjectId is null or empty in Checkout GET action. User might not be fully authenticated or claims are missing.");
            TempData["ErrorMessage"] = "Unable to identify your account. Please try logging out and logging back in.";
            // Redirect to login or home page. Challenging here might cause loops if there's an auth issue.
            return RedirectToAction("Login", "Account"); // Or "Index", "Home"
        }
        // --- END ADDED ---

        var localUser = await _context.LocalUsers.FindAsync(userSubjectId);
        // It's possible localUser is null if the UserSyncService hasn't run yet or failed.
        // This is okay for pre-filling, but the UserSubjectId from claims is the critical part.

        _logger.LogInformation("Checkout GET: UserSubjectId being pre-filled in Order model is {UserSubjectId}", userSubjectId);

        var order = new Order
        {
            UserSubjectId = userSubjectId, // This comes from the authenticated user's claims
            ContactPhoneNumber = localUser?.PhoneNumber ?? "", // Pre-fill if local user has phone
            ShippingAddress = localUser?.DefaultShippingAddress ?? "" // Example: Pre-fill from a potential LocalUser property
        };

        return View(order);
    }

        // POST: /Order/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order) // Model binding from form
        {
            var userSubjectId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userSubjectId) || order.UserSubjectId != userSubjectId)
            {
                // Security check or error
                return Forbid();
            }

            var cart = await _cartService.GetCartAsync();
            if (cart == null || !cart.Items.Any())
            {
                ModelState.AddModelError("", "Your cart is empty.");
                return View("Checkout", order); // Redisplay checkout form with error
            }

            if (ModelState.IsValid) // Validates ShippingAddress, ContactPhoneNumber etc.
            {
                order.OrderDateUtc = DateTime.UtcNow;
                order.OrderStatus = "OrderPlaced_PendingPayment"; // Or just "Placed" for now
                order.OrderTotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);

                foreach (var cartItem in cart.Items)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        UnitPrice = cartItem.UnitPrice,
                        Quantity = cartItem.Quantity
                    });
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} placed successfully for user {UserSubjectId}", order.Id, userSubjectId);

                await _cartService.ClearCartAsync(); // Clear cart after order

                TempData["StatusMessage"] = $"Thank you! Your order #{order.Id} has been placed.";
                return RedirectToAction("Confirmation", new { orderId = order.Id });
            }

            _logger.LogWarning("PlaceOrder ModelState invalid for user {UserSubjectId}", userSubjectId);
            return View("Checkout", order); // Redisplay checkout with validation errors
        }

        // GET: /Order/Confirmation/5
        public async Task<IActionResult> Confirmation(int orderId)
        {
            var userSubjectId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var order = await _context.Orders
                                .Include(o => o.OrderItems)
                                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserSubjectId == userSubjectId);

            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }
    }
}