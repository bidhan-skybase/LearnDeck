using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Ghayal_Bhaag.Models
{
    public class Anouncement
    {
        [Key]
        public int AnouncementId { get; set; }

        public string? title { get; set; }
        public string? description { get; set; }

        [Column(TypeName = "Date")]
        public DateTime StartDate { get; set; }

        [Column(TypeName = "Date")]
        public DateTime EndDate { get; set; }

    }
}
