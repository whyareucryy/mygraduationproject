using System.ComponentModel.DataAnnotations;

namespace ComputerRepairService.Models.ViewModels
{
    public class TechnicianCreateViewModel
    {
        [Required(ErrorMessage = "Укажите имя")]
        [StringLength(50)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите фамилию")]
        [StringLength(50)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите email")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [StringLength(100)]
        [Display(Name = "Email (логин для входа)")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Телефон")]
        public string? Phone { get; set; }

        [StringLength(100)]
        [Display(Name = "Специализация")]
        public string? Specialization { get; set; }

        [Display(Name = "Часовая ставка (₽)")]
        [Range(0, 999999.99)]
        public decimal? HourlyRate { get; set; }

        [Display(Name = "Сотрудник активен")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Укажите пароль")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть не короче 6 символов")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль для входа")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтвердите пароль")]
        [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
        [DataType(DataType.Password)]
        [Display(Name = "Подтверждение пароля")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
