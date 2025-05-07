using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hetro.Models
{
    public class LocalUser
    {
        [Key] 
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Don't auto-generate
        public string SubjectId { get; set; } = null!; // From IdP 'sub' claim

        public string? Email { get; set; } // From IdP 'email' claim
        public bool EmailVerified { get; set; } // From IdP 'email_verified' claim
        public string? Name { get; set; } // From IdP 'name' claim
        public string? DefaultShippingAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime LastLoginUtc { get; set; }

        // Navigation property for cart (defined next)
        public virtual ShoppingCart? Cart { get; set; }
    }
}