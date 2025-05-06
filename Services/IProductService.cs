// File: Hetro/Services/IProductService.cs
using Hetro.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hetro.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
    }
}