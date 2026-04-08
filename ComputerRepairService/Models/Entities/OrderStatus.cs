using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ComputerRepairService.Models.Entities
{
    public class OrderStatus
    {
        [Key]
        public int StatusId { get; set; }

        [Required]
        [StringLength(50)]
        public string StatusName { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<ServiceOrder> ServiceOrders { get; set; }
        public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; }
    }
}