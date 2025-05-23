﻿using BookMart.Areas.Identity.Data;
using BookMart.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookMart.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [ForeignKey("ApplicationUser")]
        [Required]
        public string UserId { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;

        [Column(TypeName = "Date")]
        public DateTime? CreatedDate { get; set; }
       

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountApplied { get; set; }

        public OrderStatus Status { get; set; }

        [NotMapped]
        public virtual IEnumerable<OrderItem>? OrderItems { get; set; }
    }
}