using Ghayal_Bhaag.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ghayal_Bhaag.Models;

namespace Ghayal_Bhaag.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Book> Book { get; set; }
        public DbSet<Ghayal_Bhaag.Models.Anouncement> Anouncement { get; set; } = default!;
        public DbSet<Ghayal_Bhaag.Models.Bookmark> Bookmark { get; set; } = default!;
        public DbSet<Ghayal_Bhaag.Models.CartItem> CartItem { get; set; } = default!;
        public DbSet<Ghayal_Bhaag.Models.OrderItem> OrderItem { get; set; } = default!;
        public DbSet<Ghayal_Bhaag.Models.Order> Order { get; set; } = default!;
        public DbSet<Ghayal_Bhaag.Models.Review> Review { get; set; } = default!;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            // Remove Database.EnsureCreated() for now; we'll use migrations instead
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Database=management_system;Username=postgres;Password=1234;");
            }
        }
    }
}