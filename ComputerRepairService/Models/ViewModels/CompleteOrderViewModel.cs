using System.ComponentModel.DataAnnotations;

namespace ComputerRepairService.Models.ViewModels
{
    public class CompleteOrderViewModel
    {
        public int OrderId { get; set; }

        [Display(Name = "Клиент")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Устройство")]
        public string DeviceDescription { get; set; } = string.Empty;

        [Display(Name = "Описание проблемы")]
        public string ProblemDescription { get; set; } = string.Empty;

        [Display(Name = "Текущий статус")]
        public string CurrentStatusName { get; set; } = string.Empty;

        [Display(Name = "Запчасти (автоматически)")]
        public decimal PartsTotal { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Стоимость работ должна быть от 0 до 999 999,99")]
        [Display(Name = "Стоимость работ (без запчастей), ₽")]
        public decimal LaborCost { get; set; }

        [Display(Name = "Итого к оплате, ₽")]
        public decimal TotalCost => PartsTotal + LaborCost;

        [StringLength(1000)]
        [Display(Name = "Комментарий мастера")]
        public string? TechnicianNotes { get; set; }
    }
}
