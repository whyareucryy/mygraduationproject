using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    [Authorize] // Все методы требуют авторизации
    public class ServiceOrdersController : Controller
    {
        private readonly RepairDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceOrdersController(RepairDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ServiceOrders
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin") || roles.Contains("Employee"))
            {
                // Админ и сотрудники видят ВСЕ заказы
                var serviceOrders = await _context.ServiceOrders
                    .Include(so => so.Customer)
                    .Include(so => so.DeviceType)
                    .Include(so => so.OrderStatus)
                    .Include(so => so.OrderTechnicians)
                        .ThenInclude(ot => ot.Technician)
                    .OrderByDescending(so => so.CreatedDate)
                    .ToListAsync();

                return View(serviceOrders);
            }
            else if (roles.Contains("Client"))
            {
                // Клиенты видят ТОЛЬКО СВОИ заказы
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Профиль клиента не найден. Обратитесь к администратору.";
                    return RedirectToAction("Index", "Home");
                }

                var serviceOrders = await _context.ServiceOrders
                    .Where(so => so.CustomerId == customer.CustomerId)
                    .Include(so => so.Customer)
                    .Include(so => so.DeviceType)
                    .Include(so => so.OrderStatus)
                    .Include(so => so.OrderTechnicians)
                        .ThenInclude(ot => ot.Technician)
                    .OrderByDescending(so => so.CreatedDate)
                    .ToListAsync();

                return View("ClientOrders", serviceOrders);
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // GET: ServiceOrders/MyOrders - ТОЛЬКО для клиентов
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Профиль клиента не найден. Обратитесь к администратору.";
                return RedirectToAction("Index", "Home");
            }

            var serviceOrders = await _context.ServiceOrders
                .Where(so => so.CustomerId == customer.CustomerId)
                .Include(so => so.Customer)
                .Include(so => so.DeviceType)
                .Include(so => so.OrderStatus)
                .Include(so => so.OrderTechnicians)
                    .ThenInclude(ot => ot.Technician)
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();

            return View(serviceOrders);
        }

        // GET: ServiceOrders/MyAssignedOrders - ТОЛЬКО для сотрудников
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyAssignedOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            var technician = await _context.Technicians
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (technician == null)
            {
                TempData["ErrorMessage"] = "Профиль мастера не найден. Обратитесь к администратору.";
                return RedirectToAction("Index", "Home");
            }

            var serviceOrders = await _context.ServiceOrders
                .Where(so => so.OrderTechnicians.Any(ot => ot.TechnicianId == technician.TechnicianId))
                .Include(so => so.Customer)
                .Include(so => so.DeviceType)
                .Include(so => so.OrderStatus)
                .Include(so => so.OrderTechnicians)
                    .ThenInclude(ot => ot.Technician)
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();

            return View(serviceOrders);
        }

        // GET: ServiceOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.Customer)
                .Include(so => so.DeviceType)
                .Include(so => so.OrderStatus)
                .Include(so => so.OrderTechnicians)
                    .ThenInclude(ot => ot.Technician)
                .Include(so => so.OrderServices)
                    .ThenInclude(os => os.Service)
                .Include(so => so.OrderParts)
                    .ThenInclude(op => op.Inventory)
                .Include(so => so.Payments)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (serviceOrder == null)
            {
                return NotFound();
            }

            // Проверка прав доступа
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin") || roles.Contains("Employee"))
            {
                // Админ и сотрудники видят все
                return View(serviceOrder);
            }
            else if (roles.Contains("Client"))
            {
                // Клиенты видят только свои заказы
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer != null && serviceOrder.CustomerId == customer.CustomerId)
                {
                    return View("ClientOrderDetails", serviceOrder);
                }
                else
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }

            return RedirectToAction("AccessDenied", "Home");
        }

        // GET: ServiceOrders/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Client"))
            {
                // Для клиента автоматически подставляем его данные
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Профиль клиента не найден. Сначала заполните профиль.";
                    return RedirectToAction("Create", "Customers");
                }

                ViewBag.CustomerId = customer.CustomerId;
                ViewBag.CustomerName = $"{customer.FirstName} {customer.LastName}";
            }

            await LoadCreateViewData();
            return View();
        }

        // POST: ServiceOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CustomerId,DeviceTypeId,DeviceBrand,DeviceModel,SerialNumber,ProblemDescription,DiagnosticNotes,StatusId,Priority,EstimatedCompletionDate,TechnicianNotes")]
            ServiceOrder serviceOrder,
            int[] selectedTechnicians)
        {
            Console.WriteLine($"=== CREATE ORDER STARTED ===");

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            // Для клиентов: автоматически подставляем CustomerId из профиля
            if (roles.Contains("Client"))
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer != null)
                {
                    serviceOrder.CustomerId = customer.CustomerId;
                }
            }

            Console.WriteLine($"CustomerId: {serviceOrder.CustomerId}, DeviceTypeId: {serviceOrder.DeviceTypeId}, StatusId: {serviceOrder.StatusId}");

            // ВАЖНО: Отключаем валидацию навигационных свойств
            ModelState.Remove("Customer");
            ModelState.Remove("DeviceType");
            ModelState.Remove("OrderStatus");
            ModelState.Remove("OrderTechnicians");
            ModelState.Remove("OrderServices");
            ModelState.Remove("OrderParts");
            ModelState.Remove("OrderStatusHistories");
            ModelState.Remove("Payments");

            // Проверяем только нужные поля
            if (serviceOrder.CustomerId == 0)
                ModelState.AddModelError("CustomerId", "Выберите клиента");

            if (serviceOrder.DeviceTypeId == 0)
                ModelState.AddModelError("DeviceTypeId", "Выберите тип устройства");

            if (serviceOrder.StatusId == 0)
                serviceOrder.StatusId = 1; // Статус "Новая" по умолчанию

            if (string.IsNullOrEmpty(serviceOrder.ProblemDescription))
                ModelState.AddModelError("ProblemDescription", "Описание проблемы обязательно");

            // Проверяем, есть ли ошибки после наших проверок
            var hasErrors = ModelState.Values.Any(v => v.Errors.Count > 0);

            if (!hasErrors)
            {
                try
                {
                    // Устанавливаем обязательные поля
                    serviceOrder.CreatedDate = DateTime.Now;

                    // Для клиентов: приоритет по умолчанию
                    if (serviceOrder.Priority == 0)
                        serviceOrder.Priority = 2;

                    // Для клиентов: нельзя выбирать статус кроме "Новая"
                    if (roles.Contains("Client"))
                    {
                        serviceOrder.StatusId = 1; // Только "Новая"
                    }

                    _context.Add(serviceOrder);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Order created with ID: {serviceOrder.OrderId}");

                    // Добавляем назначенных мастеров (только для админов и сотрудников)
                    if ((roles.Contains("Admin") || roles.Contains("Employee")) &&
                        selectedTechnicians != null && selectedTechnicians.Length > 0)
                    {
                        Console.WriteLine($"Adding {selectedTechnicians.Length} technicians to order");

                        foreach (var techId in selectedTechnicians)
                        {
                            var orderTechnician = new OrderTechnician
                            {
                                OrderId = serviceOrder.OrderId,
                                TechnicianId = techId,
                                AssignedDate = DateTime.Now,
                                IsPrimary = true
                            };
                            _context.Add(orderTechnician);
                        }
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Technicians added successfully");
                    }

                    // Добавляем запись в историю статусов
                    var statusHistory = new OrderStatusHistory
                    {
                        OrderId = serviceOrder.OrderId,
                        StatusId = serviceOrder.StatusId,
                        ChangedDate = DateTime.Now,
                        ChangedBy = User.Identity?.Name ?? "System",
                        Notes = roles.Contains("Client") ? "Заказ создан клиентом" : "Заказ создан"
                    };
                    _context.OrderStatusHistory.Add(statusHistory);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Заказ успешно создан!";
                    Console.WriteLine("=== CREATE ORDER COMPLETED SUCCESSFULLY ===");

                    if (roles.Contains("Client"))
                    {
                        return RedirectToAction(nameof(MyOrders));
                    }
                    else
                    {
                        return RedirectToAction(nameof(Details), new { id = serviceOrder.OrderId });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR creating service order: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка при создании заказа: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Validation failed - showing errors to user");
            }

            await LoadCreateViewData();
            Console.WriteLine("=== CREATE ORDER FAILED ===");
            return View(serviceOrder);
        }

        // GET: ServiceOrders/Edit/5 - ТОЛЬКО для админов и сотрудников
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.OrderTechnicians)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (serviceOrder == null)
            {
                return NotFound();
            }

            await LoadCreateViewData();
            return View(serviceOrder);
        }

        // POST: ServiceOrders/Edit/5 - ТОЛЬКО для админов и сотрудников
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("OrderId,CustomerId,DeviceTypeId,DeviceBrand,DeviceModel,SerialNumber,ProblemDescription,DiagnosticNotes,StatusId,Priority,EstimatedCompletionDate,ActualCompletionDate,TechnicianNotes,CreatedDate")]
            ServiceOrder serviceOrder,
            int[] selectedTechnicians)
        {
            Console.WriteLine($"=== EDIT ORDER STARTED ===");
            Console.WriteLine($"Order ID: {id}, Model ID: {serviceOrder.OrderId}");

            if (id != serviceOrder.OrderId)
            {
                Console.WriteLine("ID mismatch");
                return NotFound();
            }

            // Отключаем валидацию навигационных свойств
            ModelState.Remove("Customer");
            ModelState.Remove("DeviceType");
            ModelState.Remove("OrderStatus");
            ModelState.Remove("OrderTechnicians");
            ModelState.Remove("OrderServices");
            ModelState.Remove("OrderParts");
            ModelState.Remove("OrderStatusHistories");
            ModelState.Remove("Payments");

            // Проверяем обязательные поля
            if (serviceOrder.CustomerId == 0)
                ModelState.AddModelError("CustomerId", "Выберите клиента");

            if (serviceOrder.DeviceTypeId == 0)
                ModelState.AddModelError("DeviceTypeId", "Выберите тип устройства");

            if (serviceOrder.StatusId == 0)
                ModelState.AddModelError("StatusId", "Выберите статус");

            if (string.IsNullOrEmpty(serviceOrder.ProblemDescription))
                ModelState.AddModelError("ProblemDescription", "Описание проблемы обязательно");

            // Проверяем, есть ли ошибки
            var hasErrors = ModelState.Values.Any(v => v.Errors.Count > 0);

            if (!hasErrors)
            {
                try
                {
                    // Находим существующий заказ
                    var existingOrder = await _context.ServiceOrders
                        .Include(so => so.OrderTechnicians)
                        .FirstOrDefaultAsync(so => so.OrderId == id);

                    if (existingOrder == null)
                    {
                        return NotFound();
                    }

                    // Обновляем поля
                    existingOrder.CustomerId = serviceOrder.CustomerId;
                    existingOrder.DeviceTypeId = serviceOrder.DeviceTypeId;
                    existingOrder.DeviceBrand = serviceOrder.DeviceBrand;
                    existingOrder.DeviceModel = serviceOrder.DeviceModel;
                    existingOrder.SerialNumber = serviceOrder.SerialNumber;
                    existingOrder.ProblemDescription = serviceOrder.ProblemDescription;
                    existingOrder.DiagnosticNotes = serviceOrder.DiagnosticNotes;
                    existingOrder.StatusId = serviceOrder.StatusId;
                    existingOrder.Priority = serviceOrder.Priority;
                    existingOrder.EstimatedCompletionDate = serviceOrder.EstimatedCompletionDate;
                    existingOrder.TechnicianNotes = serviceOrder.TechnicianNotes;

                    // Обновляем ActualCompletionDate только если статус "Готово" и его еще нет
                    if (serviceOrder.StatusId == 5 && existingOrder.ActualCompletionDate == null)
                    {
                        existingOrder.ActualCompletionDate = DateTime.Now;
                    }

                    // Обновляем мастеров
                    // Удаляем текущих мастеров
                    _context.OrderTechnicians.RemoveRange(existingOrder.OrderTechnicians);

                    // Добавляем выбранных мастеров
                    if (selectedTechnicians != null && selectedTechnicians.Length > 0)
                    {
                        foreach (var techId in selectedTechnicians)
                        {
                            var orderTechnician = new OrderTechnician
                            {
                                OrderId = existingOrder.OrderId,
                                TechnicianId = techId,
                                AssignedDate = DateTime.Now,
                                IsPrimary = true
                            };
                            _context.OrderTechnicians.Add(orderTechnician);
                        }
                    }

                    // Добавляем запись в историю статусов если статус изменился
                    var currentStatus = await _context.ServiceOrders
                        .Where(so => so.OrderId == id)
                        .Select(so => so.StatusId)
                        .FirstOrDefaultAsync();

                    if (currentStatus != serviceOrder.StatusId)
                    {
                        var statusHistory = new OrderStatusHistory
                        {
                            OrderId = id,
                            StatusId = serviceOrder.StatusId,
                            ChangedDate = DateTime.Now,
                            ChangedBy = User.Identity?.Name ?? "System",
                            Notes = "Статус изменен при редактировании заказа"
                        };
                        _context.OrderStatusHistory.Add(statusHistory);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Заказ успешно обновлен!";
                    Console.WriteLine("=== EDIT ORDER COMPLETED SUCCESSFULLY ===");
                    return RedirectToAction(nameof(Details), new { id = existingOrder.OrderId });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"Concurrency error: {ex.Message}");
                    if (!ServiceOrderExists(serviceOrder.OrderId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR updating order: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка при обновлении заказа: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Validation failed in Edit");
            }

            // Если дошли сюда, перезагружаем данные для формы
            await LoadCreateViewData();

            // Перезагружаем OrderTechnicians для модели
            serviceOrder.OrderTechnicians = await _context.OrderTechnicians
                .Where(ot => ot.OrderId == id)
                .ToListAsync();

            Console.WriteLine("=== EDIT ORDER FAILED ===");
            return View(serviceOrder);
        }

        // GET: ServiceOrders/Delete/5 - ТОЛЬКО для админов
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.Customer)
                .Include(so => so.DeviceType)
                .Include(so => so.OrderStatus)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (serviceOrder == null)
            {
                return NotFound();
            }

            return View(serviceOrder);
        }

        // POST: ServiceOrders/Delete/5 - ТОЛЬКО для админов
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Console.WriteLine($"=== DELETE ORDER STARTED ===");
            try
            {
                var serviceOrder = await _context.ServiceOrders
                    .Include(so => so.OrderTechnicians)
                    .Include(so => so.OrderServices)
                    .Include(so => so.OrderParts)
                    .Include(so => so.OrderStatusHistories)
                    .Include(so => so.Payments)
                    .FirstOrDefaultAsync(m => m.OrderId == id);

                if (serviceOrder != null)
                {
                    // Удаляем связанные записи
                    _context.OrderTechnicians.RemoveRange(serviceOrder.OrderTechnicians);
                    _context.OrderServices.RemoveRange(serviceOrder.OrderServices);
                    _context.OrderParts.RemoveRange(serviceOrder.OrderParts);
                    _context.OrderStatusHistory.RemoveRange(serviceOrder.OrderStatusHistories);
                    _context.Payments.RemoveRange(serviceOrder.Payments);

                    // Удаляем основной заказ
                    _context.ServiceOrders.Remove(serviceOrder);

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Заказ успешно удален!";
                    Console.WriteLine("=== DELETE ORDER COMPLETED SUCCESSFULLY ===");
                }
                else
                {
                    Console.WriteLine("Order not found for deletion");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR deleting order: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при удалении заказа: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: ServiceOrders/ChangeStatus/5 - ТОЛЬКО для админов и сотрудников
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ChangeStatus(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceOrder = await _context.ServiceOrders
                .Include(so => so.OrderStatus)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (serviceOrder == null)
            {
                return NotFound();
            }

            ViewData["Statuses"] = await _context.OrderStatuses.ToListAsync();
            return View(serviceOrder);
        }

        // POST: ServiceOrders/ChangeStatus/5 - ТОЛЬКО для админов и сотрудников
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, int statusId, string notes)
        {
            Console.WriteLine($"=== CHANGE STATUS STARTED ===");
            Console.WriteLine($"Order ID: {id}, New Status: {statusId}");

            try
            {
                var serviceOrder = await _context.ServiceOrders.FindAsync(id);
                if (serviceOrder == null)
                {
                    Console.WriteLine("Order not found");
                    return NotFound();
                }

                var oldStatusId = serviceOrder.StatusId;
                serviceOrder.StatusId = statusId;

                // Добавляем запись в историю статусов
                var statusHistory = new OrderStatusHistory
                {
                    OrderId = id,
                    StatusId = statusId,
                    ChangedDate = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "System",
                    Notes = notes ?? $"Статус изменен с {oldStatusId} на {statusId}"
                };

                _context.OrderStatusHistory.Add(statusHistory);

                // Если статус "Готово", устанавливаем дату завершения
                if (statusId == 5) // Готово
                {
                    serviceOrder.ActualCompletionDate = DateTime.Now;
                    Console.WriteLine("Setting completion date for finished order");
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Статус заказа успешно обновлен!";
                Console.WriteLine("=== CHANGE STATUS COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR changing status: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при изменении статуса: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            Console.WriteLine($"=== CANCEL ORDER STARTED ===");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Профиль клиента не найден";
                    return RedirectToAction("AccessDenied", "Home");
                }

                var order = await _context.ServiceOrders.FindAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                // Проверяем, что заказ принадлежит клиенту
                if (order.CustomerId != customer.CustomerId)
                {
                    TempData["ErrorMessage"] = "Вы можете отменять только свои заказы";
                    return RedirectToAction("AccessDenied", "Home");
                }

                // Проверяем, можно ли отменить заказ
                if (order.StatusId != 1 && order.StatusId != 2) // Только "Новая" и "Диагностика"
                {
                    TempData["ErrorMessage"] = "Невозможно отменить заказ на этой стадии";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Меняем статус на "Отменено" (7)
                var oldStatusId = order.StatusId;
                order.StatusId = 7; // Отменено

                // Добавляем запись в историю статусов
                var statusHistory = new OrderStatusHistory
                {
                    OrderId = id,
                    StatusId = 7,
                    ChangedDate = DateTime.Now,
                    ChangedBy = User.Identity?.Name ?? "System",
                    Notes = "Заказ отменен клиентом"
                };
                _context.OrderStatusHistory.Add(statusHistory);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Заказ успешно отменен!";
                Console.WriteLine("=== CANCEL ORDER COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR cancelling order: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при отмене заказа: {ex.Message}";
            }

            return RedirectToAction(nameof(MyOrders));
        }

        // Вспомогательный метод для загрузки данных в формы
        private async Task LoadCreateViewData()
        {
            try
            {
                ViewData["Customers"] = await _context.Customers.Where(c => c.IsActive).ToListAsync();
                ViewData["DeviceTypes"] = await _context.DeviceTypes.ToListAsync();
                ViewData["Statuses"] = await _context.OrderStatuses.ToListAsync();
                ViewData["Technicians"] = await _context.Technicians.Where(t => t.IsActive).ToListAsync();

                // Для клиентов: скрываем мастеров (они назначаются администрацией)
                var user = await _userManager.GetUserAsync(User);
                if (User.IsInRole("Client"))
                {
                    ViewData["ShowTechnicians"] = false;
                }
                else
                {
                    ViewData["ShowTechnicians"] = true;
                }

                Console.WriteLine($"ViewData loaded - Customers: {ViewData["Customers"]}, DeviceTypes: {ViewData["DeviceTypes"]}, Statuses: {ViewData["Statuses"]}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading ViewData: {ex.Message}");
            }
        }

        private bool ServiceOrderExists(int id)
        {
            return _context.ServiceOrders.Any(e => e.OrderId == id);
        }
    }
}