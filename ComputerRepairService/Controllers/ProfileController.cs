using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ComputerRepairService.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RepairDbContext _context;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RepairDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // Главная страница - редирект на соответствующий кабинет
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Identity");
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
                return RedirectToAction("Admin");
            else if (roles.Contains("Employee"))
                return RedirectToAction("Employee");
            else if (roles.Contains("Client"))
                return RedirectToAction("Client");

            return RedirectToAction("AccessDenied", "Home");
        }

        // Личный кабинет АДМИНИСТРАТОРА
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Статистика для админа
            var model = new AdminDashboardViewModel
            {
                User = user,
                TotalOrders = await _context.ServiceOrders.CountAsync(),
                ActiveOrders = await _context.ServiceOrders
                    .Where(o => o.StatusId != 5 && o.StatusId != 6 && o.StatusId != 7) // Не готовые, выданные или отмененные
                    .CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(),
                ActiveTechnicians = await _context.Technicians.CountAsync(t => t.IsActive),
                LowStockItems = await _context.Inventory
                    .Where(i => i.QuantityInStock <= i.ReorderLevel && i.IsActive)
                    .CountAsync(),
                TotalRevenue = await _context.Payments
                    .Where(p => p.Status == "Completed")
                    .SumAsync(p => p.Amount),
                RecentOrders = await _context.ServiceOrders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderStatus)
                    .OrderByDescending(o => o.CreatedDate)
                    .Take(5)
                    .ToListAsync(),
                LowStockAlerts = await _context.Inventory
                    .Where(i => i.QuantityInStock <= i.ReorderLevel && i.IsActive)
                    .Include(i => i.PartCategory)
                    .Take(5)
                    .ToListAsync(),
                RecentCustomers = await _context.Customers
                    .OrderByDescending(c => c.RegistrationDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        // Личный кабинет СОТРУДНИКА
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Employee()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Находим связанного техника
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (technician == null)
            {
                TempData["ErrorMessage"] = "Профиль мастера не найден. Обратитесь к администратору.";
                return RedirectToAction("Index", "Home");
            }

            // Статистика для сотрудника
            var model = new EmployeeDashboardViewModel
            {
                User = user,
                Technician = technician,
                AssignedOrders = await _context.ServiceOrders
                    .Where(o => o.OrderTechnicians.Any(ot => ot.TechnicianId == technician.TechnicianId))
                    .CountAsync(),
                ActiveAssignedOrders = await _context.ServiceOrders
                    .Where(o => o.OrderTechnicians.Any(ot => ot.TechnicianId == technician.TechnicianId) &&
                               (o.StatusId == 2 || o.StatusId == 3 || o.StatusId == 4)) // Диагностика, Ожидание запчастей, В ремонте
                    .CountAsync(),
                CompletedOrdersThisMonth = await _context.ServiceOrders
                    .Where(o => o.OrderTechnicians.Any(ot => ot.TechnicianId == technician.TechnicianId) &&
                               o.StatusId == 5 && // Готово
                               o.ActualCompletionDate.HasValue &&
                               o.ActualCompletionDate.Value.Month == DateTime.Now.Month &&
                               o.ActualCompletionDate.Value.Year == DateTime.Now.Year)
                    .CountAsync(),
                TotalHoursWorked = await _context.OrderTechnicians
                    .Where(ot => ot.TechnicianId == technician.TechnicianId)
                    .SumAsync(ot => ot.HoursWorked),
                MyActiveOrders = await _context.ServiceOrders
                    .Where(o => o.OrderTechnicians.Any(ot => ot.TechnicianId == technician.TechnicianId) &&
                               o.StatusId != 5 && o.StatusId != 6 && o.StatusId != 7) // Не завершенные
                    .Include(o => o.Customer)
                    .Include(o => o.OrderStatus)
                    .OrderByDescending(o => o.Priority)
                    .ThenByDescending(o => o.CreatedDate)
                    .Take(10)
                    .ToListAsync(),
                RecentServices = await _context.OrderServices
                    .Where(os => os.TechnicianId == technician.TechnicianId)
                    .Include(os => os.ServiceOrder)
                    .Include(os => os.Service)
                    .OrderByDescending(os => os.ServiceDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        // Личный кабинет КЛИЕНТА
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Client()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Находим связанного клиента
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Профиль клиента не найден. Заполните информацию о себе.";
                return RedirectToAction("Edit", "Customers");
            }

            // Статистика для клиента
            var model = new ClientDashboardViewModel
            {
                User = user,
                Customer = customer,
                TotalOrders = await _context.ServiceOrders
                    .CountAsync(o => o.CustomerId == customer.CustomerId),
                ActiveOrders = await _context.ServiceOrders
                    .Where(o => o.CustomerId == customer.CustomerId &&
                               o.StatusId != 5 && o.StatusId != 6 && o.StatusId != 7) // Не завершенные
                    .CountAsync(),
                CompletedOrders = await _context.ServiceOrders
                    .Where(o => o.CustomerId == customer.CustomerId && o.StatusId == 5)
                    .CountAsync(),
                TotalSpent = await _context.ServiceOrders
                    .Where(o => o.CustomerId == customer.CustomerId)
                    .SumAsync(o => o.TotalCost),
                RecentOrders = await _context.ServiceOrders
                    .Where(o => o.CustomerId == customer.CustomerId)
                    .Include(o => o.OrderStatus)
                    .Include(o => o.DeviceType)
                    .OrderByDescending(o => o.CreatedDate)
                    .Take(5)
                    .ToListAsync(),
                RecentPayments = await _context.Payments
                    .Where(p => p.ServiceOrder.CustomerId == customer.CustomerId)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        // Редактирование профиля
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Обновляем данные пользователя
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            // Если email изменился
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            // Сохраняем изменения
            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                // Обновляем соответствующие бизнес-сущности
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Client"))
                {
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == user.Id);

                    if (customer != null)
                    {
                        customer.FirstName = model.FirstName;
                        customer.LastName = model.LastName;
                        customer.Email = model.Email;
                        customer.Phone = model.PhoneNumber;
                        customer.Address = model.Address;
                        await _context.SaveChangesAsync();
                    }
                }
                else if (roles.Contains("Employee"))
                {
                    var technician = await _context.Technicians
                        .FirstOrDefaultAsync(t => t.UserId == user.Id);

                    if (technician != null)
                    {
                        technician.FirstName = model.FirstName;
                        technician.LastName = model.LastName;
                        technician.Email = model.Email;
                        technician.Phone = model.PhoneNumber;
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Профиль успешно обновлен!";
                return RedirectToAction("Index");
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Смена пароля
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user, model.OldPassword, model.NewPassword);

            if (changePasswordResult.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Пароль успешно изменен!";
                return RedirectToAction("Index");
            }

            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Техническая информация о пользователе
        public async Task<IActionResult> AccountInfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var logins = await _userManager.GetLoginsAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var model = new AccountInfoViewModel
            {
                User = user,
                Roles = roles.ToList(),
                Logins = logins.ToList(),
                Claims = claims.ToList(),
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                RegistrationDate = user.RegistrationDate
            };

            return View(model);
        }
    }

    // ViewModels
    public class AdminDashboardViewModel
    {
        public ApplicationUser User { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveTechnicians { get; set; }
        public int LowStockItems { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<ServiceOrder> RecentOrders { get; set; }
        public List<Inventory> LowStockAlerts { get; set; }
        public List<Customer> RecentCustomers { get; set; }
    }

    public class EmployeeDashboardViewModel
    {
        public ApplicationUser User { get; set; }
        public Technician Technician { get; set; }
        public int AssignedOrders { get; set; }
        public int ActiveAssignedOrders { get; set; }
        public int CompletedOrdersThisMonth { get; set; }
        public decimal TotalHoursWorked { get; set; }
        public List<ServiceOrder> MyActiveOrders { get; set; }
        public List<OrderService> RecentServices { get; set; }
    }

    public class ClientDashboardViewModel
    {
        public ApplicationUser User { get; set; }
        public Customer Customer { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public List<ServiceOrder> RecentOrders { get; set; }
        public List<Payment> RecentPayments { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [Display(Name = "Имя")]
        [StringLength(50, ErrorMessage = "Имя не должно превышать 50 символов")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        [Display(Name = "Фамилия")]
        [StringLength(50, ErrorMessage = "Фамилия не должна превышать 50 символов")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Адрес")]
        [StringLength(500, ErrorMessage = "Адрес не должен превышать 500 символов")]
        public string Address { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Текущий пароль обязателен")]
        [DataType(DataType.Password)]
        [Display(Name = "Текущий пароль")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [StringLength(100, ErrorMessage = "Пароль должен содержать не менее {2} и не более {1} символов", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Новый пароль")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Подтвердите новый пароль")]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }
    }

    public class AccountInfoViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; }
        public List<UserLoginInfo> Logins { get; set; }
        public List<System.Security.Claims.Claim> Claims { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}