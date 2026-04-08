using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities
{
    public class OrderStatusHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int OrderId { get; set; }
        public int StatusId { get; set; }

        public DateTime ChangedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string ChangedBy { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual ServiceOrder ServiceOrder { get; set; }

        [ForeignKey("StatusId")]
        public virtual OrderStatus OrderStatus { get; set; }
    }
}