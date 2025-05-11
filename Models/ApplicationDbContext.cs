using BookMart.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace BookMart.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Book> Book { get; set; }
        public DbSet<BookMart.Models.Anouncement> Anouncement { get; set; } = default!;
        public DbSet<BookMart.Models.Bookmark> Bookmark { get; set; } = default!;
        public DbSet<BookMart.Models.CartItem> CartItem { get; set; } = default!;
        public DbSet<BookMart.Models.OrderItem> OrderItem { get; set; } = default!;
        public DbSet<BookMart.Models.Order> Order { get; set; } = default!;
        public DbSet<BookMart.Models.Review> Review { get; set; } = default!;

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