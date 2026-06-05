using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Services
{
    public class ClientProvisioningService : IClientProvisioningService
    {
        private const string ClientRole = "Client";
        private const string DefaultPhonePlaceholder = "Не указан";

        private readonly RepairDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<ClientProvisioningService> _logger;

        public ClientProvisioningService(
            RepairDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<ClientProvisioningService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<(bool Success, string? ErrorMessage)> ProvisionAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return (false, "Email пользователя не указан.");
            }

            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Contains("Admin") || existingRoles.Contains("Employee"))
            {
                _logger.LogInformation(
                    "Пропуск назначения роли Client для пользователя {Email}: уже есть служебная роль.",
                    user.Email);
                return (true, null);
            }

            if (!await _roleManager.RoleExistsAsync(ClientRole))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(ClientRole));
                if (!roleResult.Succeeded)
                {
                    var error = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogError("Не удалось создать роль Client: {Error}", error);
                    return (false, "Не удалось подготовить роль клиента.");
                }
            }

            if (!await _userManager.IsInRoleAsync(user, ClientRole))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, ClientRole);
                if (!addRoleResult.Succeeded)
                {
                    var error = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    _logger.LogError("Не удалось назначить роль Client пользователю {Email}: {Error}", user.Email, error);
                    return (false, "Не удалось назначить роль клиента.");
                }
            }

            var (firstName, lastName) = DeriveNamesFromEmail(user.Email);
            var shouldUpdateUser = false;

            if (string.IsNullOrWhiteSpace(user.FirstName))
            {
                user.FirstName = firstName;
                shouldUpdateUser = true;
            }

            if (string.IsNullOrWhiteSpace(user.LastName))
            {
                user.LastName = lastName;
                shouldUpdateUser = true;
            }

            if (user.RegistrationDate == default)
            {
                user.RegistrationDate = DateTime.UtcNow;
                shouldUpdateUser = true;
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id, cancellationToken);

            if (customer == null)
            {
                var customerByEmail = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Email == user.Email, cancellationToken);

                if (customerByEmail != null)
                {
                    customerByEmail.UserId = user.Id;
                    customerByEmail.FirstName = string.IsNullOrWhiteSpace(customerByEmail.FirstName)
                        ? user.FirstName ?? firstName
                        : customerByEmail.FirstName;
                    customerByEmail.LastName = string.IsNullOrWhiteSpace(customerByEmail.LastName)
                        ? user.LastName ?? lastName
                        : customerByEmail.LastName;
                    customer = customerByEmail;
                }
                else
                {
                    customer = new Customer
                    {
                        FirstName = user.FirstName ?? firstName,
                        LastName = user.LastName ?? lastName,
                        Email = user.Email,
                        Phone = string.IsNullOrWhiteSpace(user.PhoneNumber) ? DefaultPhonePlaceholder : user.PhoneNumber,
                        Address = user.Address ?? string.Empty,
                        RegistrationDate = DateTime.UtcNow,
                        IsActive = true,
                        UserId = user.Id
                    };

                    _context.Customers.Add(customer);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (user.CustomerId != customer.CustomerId)
            {
                user.CustomerId = customer.CustomerId;
                shouldUpdateUser = true;
            }

            if (shouldUpdateUser)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var error = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    _logger.LogError("Не удалось обновить профиль пользователя {Email}: {Error}", user.Email, error);
                    return (false, "Не удалось сохранить профиль клиента.");
                }
            }

            _logger.LogInformation(
                "Пользователь {Email} оформлен как клиент (CustomerId: {CustomerId}).",
                user.Email,
                customer.CustomerId);

            return (true, null);
        }

        private static (string FirstName, string LastName) DeriveNamesFromEmail(string email)
        {
            var localPart = email.Split('@')[0];
            var parts = localPart
                .Replace('.', ' ')
                .Replace('_', ' ')
                .Replace('-', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length >= 2)
            {
                return (Capitalize(parts[0]), Capitalize(parts[1]));
            }

            if (parts.Length == 1)
            {
                return (Capitalize(parts[0]), "Клиент");
            }

            return ("Новый", "Клиент");
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return char.ToUpper(value[0]) + value[1..].ToLowerInvariant();
        }
    }
}
