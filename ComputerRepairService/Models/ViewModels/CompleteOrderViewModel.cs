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

        [Required(ErrorMessage = "Укажите итоговую стоимость работ")]
        [Range(0.01, 999999.99, ErrorMessage = "Стоимость должна быть от 0,01 до 999 999,99")]
        [Display(Name = "Стоимость работ, ₽")]
        public decimal TotalCost { get; set; }

        [StringLength(1000)]
        [Display(Name = "Комментарий мастера")]
        public string? TechnicianNotes { get; set; }
    }
}
