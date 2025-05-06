// File: Hetro/Services/MockProductService.cs
using Hetro.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hetro.Services
{
    public class MockProductService : IProductService
    {
        private readonly List<Product> _products = new List<Product>
        {
            new Product { Id = 1, Name = "Classic Tee", Description = "Comfortable cotton tee.", Price = 25.99m, ImageUrl = "/images/products/tee.png" },
            new Product { Id = 2, Name = "Denim Jeans", Description = "Stylish blue denim.", Price = 59.95m, ImageUrl = "/images/products/jeans.png" },
            new Product { Id = 3, Name = "Hoodie Sweatshirt", Description = "Warm fleece hoodie.", Price = 45.00m, ImageUrl = "/images/products/hoodie.png" },
            new Product { Id = 4, Name = "Summer Dress", Description = "Light and airy dress.", Price = 75.50m, ImageUrl = "/images/products/dress.png" }
            // Add more mock products
        };

        public Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            // Simulate async operation
            return Task.FromResult(_products.AsEnumerable());
        }

        public Task<Product?> GetProductByIdAsync(int id)
        {
             // Simulate async operation
            var product = _products.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(product);
        }
    }
}