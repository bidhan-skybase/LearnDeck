using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Ghayal_Bhaag.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [ForeignKey("Order")]
        public int? OrderId { get; set; }
        public Order Order { get; set; } = null!;

        [ForeignKey("Book")]
        public int? BookId { get; set; }
        public Book Book { get; set; } = null!;

        public int Quantity { get; set; }
        public float UnitPrice { get; set; }

    }
}
