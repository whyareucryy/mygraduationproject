using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities
{
    public class OrderTechnician
    {
        [Key]
        public int OrderTechnicianId { get; set; }

        public int OrderId { get; set; }
        public int TechnicianId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(5,2)")]
        public decimal HoursWorked { get; set; } = 0;

        public bool IsPrimary { get; set; } = true;

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual ServiceOrder ServiceOrder { get; set; }

        [ForeignKey("TechnicianId")]
        public virtual Technician Technician { get; set; }
    }
}