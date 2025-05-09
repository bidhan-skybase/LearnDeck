using Ghayal_Bhaag.Areas.Identity.Data;
using Ghayal_Bhaag.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Ghayal_Bhaag.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        [ForeignKey("ApplicationUser")]
        public string? UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;


        [ForeignKey("Book")]
        public int BookId { get; set; }

        public OrderStatus? Status { get; set; }

        public int? Quantity { get; set; }
        public float? UnitPrice { get; set; }

    }
}
