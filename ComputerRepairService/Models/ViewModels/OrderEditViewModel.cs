using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.ViewModels
{
    public class OrderEditViewModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Выберите клиента")]
        [Display(Name = "Клиент")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Выберите тип устройства")]
        [Display(Name = "Тип устройства")]
        public int DeviceTypeId { get; set; }

        [Display(Name = "Бренд устройства")]
        [StringLength(50)]
        public string? DeviceBrand { get; set; }

        [Display(Name = "Модель устройства")]
        [StringLength(50)]
        public string? DeviceModel { get; set; }

        [Display(Name = "Серийный номер")]
        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [Required(ErrorMessage = "Описание проблемы обязательно")]
        [Display(Name = "Описание проблемы")]
        [StringLength(1000)]
        public string ProblemDescription { get; set; }

        [Display(Name = "Диагностические заметки")]
        [StringLength(1000)]
        public string? DiagnosticNotes { get; set; }

        [Required(ErrorMessage = "Выберите статус")]
        [Display(Name = "Статус")]
        public int StatusId { get; set; }

        [Display(Name = "Приоритет")]
        [Range(1, 5)]
        public int Priority { get; set; }

        [Display(Name = "Предполагаемая дата завершения")]
        public DateTime? EstimatedCompletionDate { get; set; }

        [Display(Name = "Фактическая дата завершения")]
        public DateTime? ActualCompletionDate { get; set; }

        [Display(Name = "Заметки мастера")]
        [StringLength(1000)]
        public string? TechnicianNotes { get; set; }

        public int[]? SelectedTechnicians { get; set; }
    }
}