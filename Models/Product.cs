// File: HetroC/Models/Product.cs
namespace Hetro.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Unnamed Product"; 
        public string Description { get; set; } = "No description available.";
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = "/images/placeholder.png"; 
    }
}