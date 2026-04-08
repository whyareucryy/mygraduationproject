using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities
{
    public class Technician
    {
        [Key]
        public int TechnicianId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(100)]
        public string Specialization { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10,2)")]
        public decimal? HourlyRate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<OrderTechnician> OrderTechnicians { get; set; }
        public virtual ICollection<OrderService> OrderServices { get; set; }

        public string? UserId { get; set; } // Внешний ключ на AspNetUsers
        public virtual ApplicationUser? User { get; set; }
    }
}