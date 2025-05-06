// Models/ShoppingCart.cs
using System.ComponentModel.DataAnnotations;

namespace Hetro.Models
{
    public class ShoppingCart
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key back to LocalUser
        [Required]
        public string UserSubjectId { get; set; } = null!;
        public virtual LocalUser User { get; set; } = null!;

        public virtual List<CartItem> Items { get; set; } = new List<CartItem>();
        public DateTime LastUpdatedUtc { get; set; }
    }
}
