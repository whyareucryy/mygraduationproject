using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Services
{
    public class AdminAccountProvisioningService : IAdminAccountProvisioningService
    {
        private const string ClientRole = "Client";
        private const string EmployeeRole = "Employee";

        private readonly RepairDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminAccountProvisioningService> _logger;

        public AdminAccountProvisioningService(
            RepairDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminAccountProvisioningService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error)> CreateCustomerWithAccountAsync(Customer customer, string password)
        {
            var email = customer.Email.Trim();

            var emailTaken = await _userManager.FindByEmailAsync(email);
            if (emailTaken != null)
            {
                return (false, $"Пользователь с email «{email}» уже существует.");
            }

            var duplicateCustomer = await _context.Customers
                .AnyAsync(c => c.Email == email);
            if (duplicateCustomer)
            {
                return (false, $"Клиент с email «{email}» уже есть в базе.");
            }

            await EnsureRoleExistsAsync(ClientRole);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            ApplicationUser? user = null;

            try
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    PhoneNumber = customer.Phone,
                    Address = customer.Address ?? string.Empty,
                    RegistrationDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    return (false, FormatErrors(createResult.Errors));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, ClientRole);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return (false, FormatErrors(roleResult.Errors));
                }

                customer.Email = email;
                customer.UserId = user.Id;
                customer.RegistrationDate = DateTime.UtcNow;
                customer.IsActive = true;

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                user.CustomerId = customer.CustomerId;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(FormatErrors(updateResult.Errors));
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Админ создал клиента {Email} с учётной записью (CustomerId: {CustomerId}).",
                    email,
                    customer.CustomerId);

                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }

                _logger.LogError(ex, "Ошибка создания клиента с учётной записью {Email}", email);
                return (false, "Не удалось создать учётную запись клиента. Попробуйте снова.");
            }
        }

        public async Task<(bool Success, string? Error)> CreateTechnicianWithAccountAsync(Technician technician, string password)
        {
            var email = technician.Email.Trim();

            var emailTaken = await _userManager.FindByEmailAsync(email);
            if (emailTaken != null)
            {
                return (false, $"Пользователь с email «{email}» уже существует.");
            }

            var duplicateTechnician = await _context.Technicians
                .AnyAsync(t => t.Email == email);
            if (duplicateTechnician)
            {
                return (false, $"Мастер с email «{email}» уже есть в базе.");
            }

            await EnsureRoleExistsAsync(EmployeeRole);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            ApplicationUser? user = null;

            try
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = technician.FirstName,
                    LastName = technician.LastName,
                    PhoneNumber = technician.Phone,
                    RegistrationDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    return (false, FormatErrors(createResult.Errors));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, EmployeeRole);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return (false, FormatErrors(roleResult.Errors));
                }

                technician.Email = email;
                technician.UserId = user.Id;
                technician.HireDate = DateTime.UtcNow;
                technician.IsActive = true;

                _context.Technicians.Add(technician);
                await _context.SaveChangesAsync();

                user.TechnicianId = technician.TechnicianId;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(FormatErrors(updateResult.Errors));
                }

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Админ создал мастера {Email} с учётной записью (TechnicianId: {TechnicianId}).",
                    email,
                    technician.TechnicianId);

                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }

                _logger.LogError(ex, "Ошибка создания мастера с учётной записью {Email}", email);
                return (false, "Не удалось создать учётную запись мастера. Попробуйте снова.");
            }
        }

        private async Task EnsureRoleExistsAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(FormatErrors(result.Errors));
                }
            }
        }

        private static string FormatErrors(IEnumerable<IdentityError> errors)
        {
            return string.Join(" ", errors.Select(e => e.Description));
        }
    }
}
