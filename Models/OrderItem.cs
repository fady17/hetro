// File: Hetro/Models/OrderItem.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hetro.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = "N/A"; // Snapshot of name at time of order
        public decimal UnitPrice { get; set; }    // Snapshot of price
        public int Quantity { get; set; }

        // Foreign Key to Order
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;
    }
}