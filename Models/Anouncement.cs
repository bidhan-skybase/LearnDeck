using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Ghayal_Bhaag.Models
{
    public class Anouncement
    {
        [Key]
        public int AnouncementId { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        [Column(TypeName = "Date")]
        public DateTime StartDate { get; set; }

        [Column(TypeName = "Date")]
        public DateTime EndDate { get; set; }

    }
}
