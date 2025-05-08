using Ghayal_Bhaag.Areas.Identity.Data;
using Ghayal_Bhaag.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Ghayal_Bhaag.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;

        [Column(TypeName = "timestamp")]
        public DateTime CreatedDate { get; set; }

        public float TotalAmount { get; set; }
        public float DiscountApplied { get; set; }
        public OrderStatus status { get; set; }

        [NotMapped]
        public virtual IEnumerable<OrderItem>? OrderItems { get; set; }

    }
}
