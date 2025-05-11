using BookMart.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookMart.Models
{
    public class Bookmark
    {
        [Key]
        public int BookmarkId { get; set; }

        [ForeignKey("Book")]
        public int? BookId { get; set; }
        public Book Book { get; set; } = null!;

        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        [Column(TypeName = "Date")]
        public DateTime? CreatedDate { get; set; }

    }
}
