// File: Hetro/Models/Order.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
//
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Hetro.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserSubjectId { get; set; } = null!; 
        [ValidateNever]
        public virtual LocalUser User { get; set; } = null!;

        public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;
        public decimal OrderTotal { get; set; }

        [Required(ErrorMessage = "Please enter your shipping address.")]
        public string ShippingAddress { get; set; } = "N/A";
        [Required(ErrorMessage = "Please enter your contact phone number.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        public string ContactPhoneNumber { get; set; } = "N/A";

        public string OrderStatus { get; set; } = "Pending"; // e.g., Pending, Processing, Shipped, Delivered

        public virtual List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}