using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Context
{
    public class ServiceContext : DbContext
    {
        public ServiceContext(DbContextOptions options) : base(options) { }

        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<ProductImage> ProductImages { get; set; }

        public DbSet<Message> Outbox { get; set; }

        override
        protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();


        }

    }
}
