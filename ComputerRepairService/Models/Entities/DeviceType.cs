using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ComputerRepairService.Models.Entities
{
    public class DeviceType
    {
        [Key]
        public int DeviceTypeId { get; set; }

        [Required]
        [StringLength(50)]
        public string TypeName { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<ServiceOrder> ServiceOrders { get; set; }
    }
}