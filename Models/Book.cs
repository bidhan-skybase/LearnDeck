using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BookMart.Enums;

namespace BookMart.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        [Required]
        public string BookTitle { get; set; } = null!;

        [Required]
        public string ISBN { get; set; } = null!;

        [Column(TypeName = "Date")]
        public DateTime? DateReleased { get; set; }

        public string? Description { get; set; }

        [Required]
        public string Publisher { get; set; } = null!;

        [Required]
        public string Genre { get; set; } = null!;

        public bool PhysicalAccess { get; set; }

        public bool Sale { get; set; }

        public bool NewArrival { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Required]
        public string Language { get; set; } = null!;

        public Format Format { get; set; }

        [Required]
        public string Author { get; set; } = null!;

        public string? Image { get; set; }
    }
}