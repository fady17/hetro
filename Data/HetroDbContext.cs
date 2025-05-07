// File: Hetro/Data/HetroDbContext.cs
using Hetro.Models;
using Microsoft.EntityFrameworkCore;

namespace Hetro.Data
{
    public class HetroDbContext : DbContext
    {
        public HetroDbContext(DbContextOptions<HetroDbContext> options) : base(options) { }

        public DbSet<LocalUser> LocalUsers { get; set; } = default!;
        public DbSet<ShoppingCart> ShoppingCarts { get; set; } = default!;
        public DbSet<CartItem> CartItems { get; set; } = default!;
        public DbSet<Product> Products { get; set; } = default!; // Add DbSet for Products
        public DbSet<Order> Orders { get; set; } = default!;     // Add DbSet for Orders
        public DbSet<OrderItem> OrderItems { get; set; } = default!; // Add DbSet for OrderItems


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LocalUser>()
                .HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<ShoppingCart>(c => c.UserSubjectId);

            modelBuilder.Entity<ShoppingCart>()
                .HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.ShoppingCartId);

            // --- Add Order Relationships ---
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany() // A user can have many orders, but an order belongs to one user
                .HasForeignKey(o => o.UserSubjectId)
                .IsRequired();

            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId);
            // --- End Order Relationships ---
        }
    }
}