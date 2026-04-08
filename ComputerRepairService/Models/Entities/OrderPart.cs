using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities
{
    public class OrderPart
    {
        [Key]
        public int OrderPartId { get; set; }

        public int OrderId { get; set; }
        public int PartId { get; set; }

        public int QuantityUsed { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPriceAtTime { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice => QuantityUsed * UnitPriceAtTime;

        public DateTime UsageDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual ServiceOrder ServiceOrder { get; set; }

        [ForeignKey("PartId")]
        public virtual Inventory Inventory { get; set; }
    }
}