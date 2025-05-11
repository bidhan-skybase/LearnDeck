using BookMart.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        [ForeignKey("Book")]
        public int? BookId { get; set; }
        public Book Book { get; set; } = null!;

        public string? description { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? CreatedDate { get; set; }

    }
}
