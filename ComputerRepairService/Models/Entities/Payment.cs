using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int OrderId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string PaymentMethod { get; set; } // 'Cash', 'Card', 'Bank Transfer'

        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // 'Pending', 'Completed', 'Failed'

        [StringLength(100)]
        public string TransactionId { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual ServiceOrder ServiceOrder { get; set; }
    }
}