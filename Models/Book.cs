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

        public string title { get; set; }

        public string ISBN { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? DateReleased { get; set; }

        public string description { get; set; }

        public string publisher { get; set; }

        public string genre { get; set; }

        public bool physical_access { get; set; }

        public bool on_sale { get; set; }

        public bool new_arrival { get; set; }

        public int stock { get; set; }

        public float price { get; set; }

        public float discount { get; set; }

        public string language { get; set; }

        public Format format { get; set; }

        // New properties
        public string author { get; set; }

        public string imageUrl { get; set; }
    }
}