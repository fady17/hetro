// File: /Controllers/ProductsController.cs
using Hetro.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Hetro.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
         private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET: /Products/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found.", id);
                return NotFound(); // Return 404 if product doesn't exist
            }
            return View(product); // Pass product to the view
        }
    }
}