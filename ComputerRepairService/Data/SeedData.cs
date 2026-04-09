using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComputerRepairService.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            Console.WriteLine("=== SIMPLE SEED DATA START ===");

            try
            {
                var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>(); // Изменили на ILogger<Program>
                var context = serviceProvider.GetRequiredService<RepairDbContext>();

                logger.LogInformation("=== ИНИЦИАЛИЗАЦИЯ IDENTITY ===");

                // 1. ТОЛЬКО РОЛИ И ПОЛЬЗОВАТЕЛИ IDENTITY
                // Создание ролей
                string[] roleNames = { "Admin", "Employee", "Client" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                        Console.WriteLine($"Роль создана: {roleName}");
                        logger.LogInformation($"Роль '{roleName}' создана");
                    }
                    else
                    {
                        Console.WriteLine($"Роль '{roleName}' уже существует");
                    }
                }

                // 2. Создание администратора
                var adminEmail = "admin@admin.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FirstName = "Главный",
                        LastName = "Администратор",
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(adminUser, "admin1234");
                    if (createResult.Succeeded)
                    {
                        Console.WriteLine($"Администратор создан: {adminEmail}");
                        logger.LogInformation($"Администратор '{adminEmail}' создан");

                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        Console.WriteLine("Роль Admin назначена");
                        logger.LogInformation($"Роль 'Admin' назначена пользователю '{adminEmail}'");
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка создания администратора: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                        logger.LogError($"Ошибка создания администратора: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"Администратор уже существует: {adminEmail}");
                    logger.LogInformation($"Администратор '{adminEmail}' уже существует");

                    // Проверяем и добавляем роль если нужно
                    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        Console.WriteLine("Роль Admin добавлена существующему пользователю");
                        logger.LogInformation($"Роль 'Admin' добавлена существующему пользователю '{adminEmail}'");
                    }
                }

                // 3. Создание тестового сотрудника
                var employeeEmail = "employee@service.com";
                var employeeUser = await userManager.FindByEmailAsync(employeeEmail);

                if (employeeUser == null)
                {
                    employeeUser = new ApplicationUser
                    {
                        UserName = employeeEmail,
                        Email = employeeEmail,
                        FirstName = "Иван",
                        LastName = "Сервисный",
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(employeeUser, "Employee123!");
                    if (createResult.Succeeded)
                    {
                        Console.WriteLine($"Сотрудник создан: {employeeEmail}");
                        logger.LogInformation($"Сотрудник '{employeeEmail}' создан");

                        await userManager.AddToRoleAsync(employeeUser, "Employee");
                        logger.LogInformation($"Роль 'Employee' назначена пользователю '{employeeEmail}'");

                        // Создаем связанного техника
                        var technician = new Technician
                        {
                            FirstName = "Иван",
                            LastName = "Сервисный",
                            Email = employeeEmail,
                            Phone = "+79991112233",
                            Specialization = "Ремонт ноутбуков",
                            HireDate = DateTime.UtcNow.AddYears(-1),
                            HourlyRate = 500.00m,
                            IsActive = true,
                            UserId = employeeUser.Id
                        };

                        // Проверяем, не существует ли уже
                        var existingTechnician = await context.Technicians
                            .FirstOrDefaultAsync(t => t.Email == employeeEmail);

                        if (existingTechnician == null)
                        {
                            context.Technicians.Add(technician);
                            await context.SaveChangesAsync();
                            Console.WriteLine("Связанный Technician создан");
                            logger.LogInformation($"Technician создан для пользователя '{employeeEmail}'");
                        }
                        else
                        {
                            Console.WriteLine("Technician уже существует для этого email");
                        }
                    }
                    else
                    {
                        logger.LogError($"Ошибка создания сотрудника: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"Сотрудник уже существует: {employeeEmail}");
                    logger.LogInformation($"Сотрудник '{employeeEmail}' уже существует");
                }

                // Для существующего/нового сотрудника всегда гарантируем роль и Technician-профиль
                if (!await userManager.IsInRoleAsync(employeeUser, "Employee"))
                {
                    await userManager.AddToRoleAsync(employeeUser, "Employee");
                    logger.LogInformation($"Роль 'Employee' назначена пользователю '{employeeEmail}'");
                }

                var linkedTechnician = await context.Technicians
                    .FirstOrDefaultAsync(t => t.UserId == employeeUser.Id);

                if (linkedTechnician == null)
                {
                    var technicianByEmail = await context.Technicians
                        .FirstOrDefaultAsync(t => t.Email == employeeEmail);

                    if (technicianByEmail != null)
                    {
                        technicianByEmail.UserId = employeeUser.Id;
                        logger.LogInformation($"Technician '{employeeEmail}' привязан к Identity UserId");
                    }
                    else
                    {
                        context.Technicians.Add(new Technician
                        {
                            FirstName = employeeUser.FirstName ?? "Сотрудник",
                            LastName = employeeUser.LastName ?? "Сервиса",
                            Email = employeeEmail,
                            Phone = "+79991112233",
                            Specialization = "Общий ремонт",
                            HireDate = DateTime.UtcNow.AddYears(-1),
                            HourlyRate = 500.00m,
                            IsActive = true,
                            UserId = employeeUser.Id
                        });
                        logger.LogInformation($"Создан Technician для существующего пользователя '{employeeEmail}'");
                    }

                    await context.SaveChangesAsync();
                }

                // 4. Создание тестового клиента
                var clientEmail = "client@example.com";
                var clientUser = await userManager.FindByEmailAsync(clientEmail);

                if (clientUser == null)
                {
                    clientUser = new ApplicationUser
                    {
                        UserName = clientEmail,
                        Email = clientEmail,
                        FirstName = "Алексей",
                        LastName = "Клиентов",
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(clientUser, "Client123!");
                    if (createResult.Succeeded)
                    {
                        Console.WriteLine($"Клиент создан: {clientEmail}");
                        logger.LogInformation($"Клиент '{clientEmail}' создан");

                        await userManager.AddToRoleAsync(clientUser, "Client");
                        logger.LogInformation($"Роль 'Client' назначена пользователю '{clientEmail}'");

                        // Создаем связанного клиента
                        var customer = new Customer
                        {
                            FirstName = "Алексей",
                            LastName = "Клиентов",
                            Email = clientEmail,
                            Phone = "+79991234567",
                            Address = "ул. Тестовая, д. 1",
                            RegistrationDate = DateTime.UtcNow.AddDays(-30),
                            IsActive = true,
                            UserId = clientUser.Id
                        };

                        // Проверяем, не существует ли уже
                        var existingCustomer = await context.Customers
                            .FirstOrDefaultAsync(c => c.Email == clientEmail);

                        if (existingCustomer == null)
                        {
                            context.Customers.Add(customer);
                            await context.SaveChangesAsync();
                            Console.WriteLine("Связанный Customer создан");
                            logger.LogInformation($"Customer создан для пользователя '{clientEmail}'");
                        }
                        else
                        {
                            Console.WriteLine("Customer уже существует для этого email");
                        }
                    }
                    else
                    {
                        logger.LogError($"Ошибка создания клиента: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"Клиент уже существует: {clientEmail}");
                    logger.LogInformation($"Клиент '{clientEmail}' уже существует");
                }

                // Для существующего/нового клиента всегда гарантируем роль и Customer-профиль
                if (!await userManager.IsInRoleAsync(clientUser, "Client"))
                {
                    await userManager.AddToRoleAsync(clientUser, "Client");
                    logger.LogInformation($"Роль 'Client' назначена пользователю '{clientEmail}'");
                }

                var linkedCustomer = await context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == clientUser.Id);

                if (linkedCustomer == null)
                {
                    var customerByEmail = await context.Customers
                        .FirstOrDefaultAsync(c => c.Email == clientEmail);

                    if (customerByEmail != null)
                    {
                        customerByEmail.UserId = clientUser.Id;
                        logger.LogInformation($"Customer '{clientEmail}' привязан к Identity UserId");
                    }
                    else
                    {
                        context.Customers.Add(new Customer
                        {
                            FirstName = clientUser.FirstName ?? "Клиент",
                            LastName = clientUser.LastName ?? "Сервиса",
                            Email = clientEmail,
                            Phone = "+79991234567",
                            Address = "ул. Тестовая, д. 1",
                            RegistrationDate = DateTime.UtcNow.AddDays(-30),
                            IsActive = true,
                            UserId = clientUser.Id
                        });
                        logger.LogInformation($"Создан Customer для существующего пользователя '{clientEmail}'");
                    }

                    await context.SaveChangesAsync();
                }

                // 5. Синхронизация legacy-профилей из БД (если UserId еще не заполнен)
                await SyncUnlinkedCustomersAsync(context, userManager, logger);
                await SyncUnlinkedTechniciansAsync(context, userManager, logger);

                Console.WriteLine("=== SEED DATA COMPLETED SUCCESSFULLY ===");
                logger.LogInformation("=== ИНИЦИАЛИЗАЦИЯ IDENTITY ЗАВЕРШЕНА УСПЕШНО ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== SEED DATA FAILED: {ex.Message} ===");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Ошибка в SeedData.Initialize");
            }
        }

        private static async Task SyncUnlinkedCustomersAsync(
            RepairDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger logger)
        {
            var unlinkedCustomers = await context.Customers
                .Where(c => string.IsNullOrWhiteSpace(c.UserId) && !string.IsNullOrWhiteSpace(c.Email))
                .ToListAsync();

            foreach (var customer in unlinkedCustomers)
            {
                var email = customer.Email.Trim();
                var user = await userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        PhoneNumber = customer.Phone,
                        Address = customer.Address,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(user, "TempClient123!");
                    if (!createResult.Succeeded)
                    {
                        logger.LogError("Не удалось создать Identity-пользователя для Customer {CustomerId} ({Email}): {Errors}",
                            customer.CustomerId,
                            email,
                            string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        continue;
                    }
                }

                if (!await userManager.IsInRoleAsync(user, "Client"))
                {
                    await userManager.AddToRoleAsync(user, "Client");
                }

                customer.UserId = user.Id;
            }

            if (unlinkedCustomers.Any())
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Синхронизированы unlinked Customers: {Count}", unlinkedCustomers.Count);
            }
        }

        private static async Task SyncUnlinkedTechniciansAsync(
            RepairDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger logger)
        {
            var unlinkedTechnicians = await context.Technicians
                .Where(t => string.IsNullOrWhiteSpace(t.UserId) && !string.IsNullOrWhiteSpace(t.Email))
                .ToListAsync();

            foreach (var technician in unlinkedTechnicians)
            {
                var email = technician.Email.Trim();
                var user = await userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = technician.FirstName,
                        LastName = technician.LastName,
                        PhoneNumber = technician.Phone,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(user, "TempEmployee123!");
                    if (!createResult.Succeeded)
                    {
                        logger.LogError("Не удалось создать Identity-пользователя для Technician {TechnicianId} ({Email}): {Errors}",
                            technician.TechnicianId,
                            email,
                            string.Join(", ", createResult.Errors.Select(e => e.Description)));
                        continue;
                    }
                }

                if (!await userManager.IsInRoleAsync(user, "Employee"))
                {
                    await userManager.AddToRoleAsync(user, "Employee");
                }

                technician.UserId = user.Id;
            }

            if (unlinkedTechnicians.Any())
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Синхронизированы unlinked Technicians: {Count}", unlinkedTechnicians.Count);
            }
        }
    }
}
