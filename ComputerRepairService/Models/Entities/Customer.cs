using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ComputerRepairService.Models.Entities
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

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

        [Required]
        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<ServiceOrder> ServiceOrders { get; set; }

        public string? UserId { get; set; } // Внешний ключ на AspNetUsers
        public virtual ApplicationUser? User { get; set; }
    }

}