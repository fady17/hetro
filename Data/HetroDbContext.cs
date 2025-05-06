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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<LocalUser>()
                .HasOne(u => u.Cart)
                .WithOne(c => c.User)
                .HasForeignKey<ShoppingCart>(c => c.UserSubjectId);

            modelBuilder.Entity<ShoppingCart>()
                .HasMany(c => c.Items)
                .WithOne(i => i.Cart)
                .HasForeignKey(i => i.ShoppingCartId);
        }
    }
}