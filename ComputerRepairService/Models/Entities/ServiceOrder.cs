// Models/Entities/ServiceOrder.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities
{
    public class ServiceOrder
    {
        [Key] // ДОБАВЬТЕ ЭТОТ АТРИБУТ
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
        public int Priority { get; set; } = 2;

        [Display(Name = "Дата создания")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Предполагаемая дата завершения")]
        public DateTime? EstimatedCompletionDate { get; set; }

        [Display(Name = "Фактическая дата завершения")]
        public DateTime? ActualCompletionDate { get; set; }

        [Display(Name = "Общая стоимость")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; set; }

        [Display(Name = "Заметки мастера")]
        [StringLength(1000)]
        public string? TechnicianNotes { get; set; }

        // Навигационные свойства
        public virtual Customer Customer { get; set; }
        public virtual DeviceType DeviceType { get; set; }
        public virtual OrderStatus OrderStatus { get; set; }
        public virtual ICollection<OrderTechnician> OrderTechnicians { get; set; } = new List<OrderTechnician>();
        public virtual ICollection<OrderService> OrderServices { get; set; } = new List<OrderService>();
        public virtual ICollection<OrderPart> OrderParts { get; set; } = new List<OrderPart>();
        public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}