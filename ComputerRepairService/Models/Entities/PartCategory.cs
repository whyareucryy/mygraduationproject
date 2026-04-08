using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ComputerRepairService.Models.Entities
{
    public class PartCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(50)]
        public string CategoryName { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<Inventory> Inventories { get; set; }
    }
}