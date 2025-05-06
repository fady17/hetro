using System.ComponentModel.DataAnnotations;

namespace Hetro.Models
 {
     public class CartItem
     {
         [Key]
         public int Id { get; set; }
         public int ProductId { get; set; } // Link to product (using mock ID for now)
         public string ProductName { get; set; } = "N/A"; // Store name/price snapshot
         public decimal UnitPrice { get; set; }
         public int Quantity { get; set; }

         // Foreign Key to Cart
         public int ShoppingCartId { get; set; }
         public virtual ShoppingCart Cart { get; set; } = null!;
     }
 }