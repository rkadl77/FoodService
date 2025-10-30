using hitsApplication.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace hitsApplication.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("cart_items");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.DishId);

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(10,2)");

                entity.Property(e => e.Name)
                    .HasMaxLength(255);

                entity.Property(e => e.Quantity)
                    .HasDefaultValue(1);

                entity.HasIndex(e => new { e.UserId, e.SessionId, e.DishId })
                    .IsUnique();
            });
        }
    }
}