using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities
{
    public class OrderService
    {
        [Key]
        public int OrderServiceId { get; set; }

        public int OrderId { get; set; }
        public int ServiceId { get; set; }
        public int TechnicianId { get; set; }

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice => Quantity * UnitPrice;

        public DateTime ServiceDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual ServiceOrder ServiceOrder { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; }

        [ForeignKey("TechnicianId")]
        public virtual Technician Technician { get; set; }
    }
}