using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Ghayal_Bhaag.Enums;

namespace Ghayal_Bhaag.Models
{
    public class Book
    {
        public Book()
        {
        }

        [Key]
        public int BookId { get; set; }

        public string BookTitle { get; set; }

        public string ISBN { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? DateReleased { get; set; }

        public string Description { get; set; }

        public string Publisher { get; set; }

        public string Genre { get; set; }

        public bool PhysicalAccess { get; set; }

        public bool Sale { get; set; }

        public bool NewArrival { get; set; }

        public int Stock { get; set; }

        public float Price { get; set; }

        public float DiscountAmount { get; set; }

        public string Language { get; set; }

        public Format Format { get; set; }

        // New properties
        public string Author { get; set; }

        public string Image { get; set; }
    }
}