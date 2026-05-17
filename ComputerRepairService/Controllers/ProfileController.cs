using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Services;
using ComputerRepairService.Models.Enums;
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

        // Главная страница - редирект на AccountInfo
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Identity");
            }

            return RedirectToAction("AccountInfo");
        }

        // Редактирование профиля (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userObj = await _userManager.GetUserAsync(User);
                if (userObj == null) return NotFound();
                
                var roles = await _userManager.GetRolesAsync(userObj);
                var logins = await _userManager.GetLoginsAsync(userObj);
                var claims = await _userManager.GetClaimsAsync(userObj);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userObj.Id);
                var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == userObj.Id);

                var fullModel = new AccountInfoViewModel
                {
                    User = userObj,
                    Roles = roles.ToList(),
                    Customer = customer,
                    Technician = technician,
                    EditProfileModel = model,
                    ChangePasswordModel = new ChangePasswordViewModel(),
                    Logins = logins.ToList(),
                    Claims = claims.ToList(),
                    EmailConfirmed = userObj.EmailConfirmed,
                    PhoneNumberConfirmed = userObj.PhoneNumberConfirmed,
                    TwoFactorEnabled = userObj.TwoFactorEnabled,
                    LockoutEnd = userObj.LockoutEnd,
                    AccessFailedCount = userObj.AccessFailedCount,
                    RegistrationDate = userObj.RegistrationDate
                };

                return View("AccountInfo", fullModel);
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
                    
                    var roles = await _userManager.GetRolesAsync(user);
                    var logins = await _userManager.GetLoginsAsync(user);
                    var claims = await _userManager.GetClaimsAsync(user);
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                    var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == user.Id);

                    var fullModel = new AccountInfoViewModel
                    {
                        User = user,
                        Roles = roles.ToList(),
                        Customer = customer,
                        Technician = technician,
                        EditProfileModel = model,
                        ChangePasswordModel = new ChangePasswordViewModel(),
                        Logins = logins.ToList(),
                        Claims = claims.ToList(),
                        EmailConfirmed = user.EmailConfirmed,
                        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                        TwoFactorEnabled = user.TwoFactorEnabled,
                        LockoutEnd = user.LockoutEnd,
                        AccessFailedCount = user.AccessFailedCount,
                        RegistrationDate = user.RegistrationDate
                    };

                    return View("AccountInfo", fullModel);
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
                return RedirectToAction("AccountInfo");
            }

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var rolesErr = await _userManager.GetRolesAsync(user);
            var loginsErr = await _userManager.GetLoginsAsync(user);
            var claimsErr = await _userManager.GetClaimsAsync(user);
            var customerErr = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            var technicianErr = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == user.Id);

            var errModel = new AccountInfoViewModel
            {
                User = user,
                Roles = rolesErr.ToList(),
                Customer = customerErr,
                Technician = technicianErr,
                EditProfileModel = model,
                ChangePasswordModel = new ChangePasswordViewModel(),
                Logins = loginsErr.ToList(),
                Claims = claimsErr.ToList(),
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                RegistrationDate = user.RegistrationDate
            };

            return View("AccountInfo", errModel);
        }

        // Смена пароля (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userObj = await _userManager.GetUserAsync(User);
                if (userObj == null) return NotFound();
                
                var roles = await _userManager.GetRolesAsync(userObj);
                var logins = await _userManager.GetLoginsAsync(userObj);
                var claims = await _userManager.GetClaimsAsync(userObj);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userObj.Id);
                var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == userObj.Id);
                
                var editModel = new EditProfileViewModel
                {
                    FirstName = userObj.FirstName,
                    LastName = userObj.LastName,
                    Email = userObj.Email,
                    PhoneNumber = userObj.PhoneNumber,
                    Address = userObj.Address
                };

                var fullModel = new AccountInfoViewModel
                {
                    User = userObj,
                    Roles = roles.ToList(),
                    Customer = customer,
                    Technician = technician,
                    EditProfileModel = editModel,
                    ChangePasswordModel = model,
                    Logins = logins.ToList(),
                    Claims = claims.ToList(),
                    EmailConfirmed = userObj.EmailConfirmed,
                    PhoneNumberConfirmed = userObj.PhoneNumberConfirmed,
                    TwoFactorEnabled = userObj.TwoFactorEnabled,
                    LockoutEnd = userObj.LockoutEnd,
                    AccessFailedCount = userObj.AccessFailedCount,
                    RegistrationDate = userObj.RegistrationDate
                };

                return View("AccountInfo", fullModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user, model.OldPassword, model.NewPassword);

            if (changePasswordResult.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Пароль успешно изменен!";
                return RedirectToAction("AccountInfo");
            }

            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var rolesErr = await _userManager.GetRolesAsync(user);
            var loginsErr = await _userManager.GetLoginsAsync(user);
            var claimsErr = await _userManager.GetClaimsAsync(user);
            var customerErr = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            var technicianErr = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == user.Id);
            
            var editModelErr = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };

            var errModel = new AccountInfoViewModel
            {
                User = user,
                Roles = rolesErr.ToList(),
                Customer = customerErr,
                Technician = technicianErr,
                EditProfileModel = editModelErr,
                ChangePasswordModel = model,
                Logins = loginsErr.ToList(),
                Claims = claimsErr.ToList(),
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                RegistrationDate = user.RegistrationDate
            };

            return View("AccountInfo", errModel);
        }

        // Техническая информация о пользователе
        public async Task<IActionResult> AccountInfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var logins = await _userManager.GetLoginsAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            var technician = await _context.Technicians.FirstOrDefaultAsync(t => t.UserId == user.Id);

            var editModel = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address
            };

            var model = new AccountInfoViewModel
            {
                User = user,
                Roles = roles.ToList(),
                Customer = customer,
                Technician = technician,
                EditProfileModel = editModel,
                ChangePasswordModel = new ChangePasswordViewModel(),
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
        public List<ComputerRepairService.Models.Entities.OrderService> RecentServices { get; set; }
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
        public int OrdersAwaitingPayment { get; set; }
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
        public Customer Customer { get; set; }
        public Technician Technician { get; set; }
        public EditProfileViewModel EditProfileModel { get; set; }
        public ChangePasswordViewModel ChangePasswordModel { get; set; }
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