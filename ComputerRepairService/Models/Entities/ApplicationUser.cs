using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComputerRepairService.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        // Дополнительные поля для профиля (общие для всех)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        // Связи с бизнес-сущностями (не обязательны для всех пользователей)
        [ForeignKey("Customer")]
        public int? CustomerId { get; set; } // Ссылка на клиента, если пользователь - клиент
        public virtual Customer? Customer { get; set; }

        [ForeignKey("Technician")]
        public int? TechnicianId { get; set; } // Ссылка на сотрудника, если пользователь - техник
        public virtual Technician? Technician { get; set; }
    }
}