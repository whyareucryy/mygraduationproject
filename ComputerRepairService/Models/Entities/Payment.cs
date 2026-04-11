using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ComputerRepairService.Models.Enums;

namespace ComputerRepairService.Models.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required(ErrorMessage = "Выберите заказ")]
        [Display(Name = "Заказ")]
        public int OrderId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Сумма")]
        public decimal Amount { get; set; }

        [Display(Name = "Дата платежа")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Выберите метод оплаты")]
        [StringLength(50)]
        [Display(Name = "Метод оплаты")]
        public string PaymentMethod { get; set; } = PaymentMethodCodes.Cash;

        [StringLength(50)]
        [Display(Name = "Статус")]
        public string Status { get; set; } = "Completed";

        [StringLength(100)]
        [Display(Name = "ID транзакции")]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        [Display(Name = "Примечания")]
        public string? Notes { get; set; }

        [ForeignKey("OrderId")]
        public virtual ServiceOrder? ServiceOrder { get; set; }
    }
}
