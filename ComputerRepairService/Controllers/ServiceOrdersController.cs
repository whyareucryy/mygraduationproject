using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.Enums;
using ComputerRepairService.Models.ViewModels;
using ComputerRepairService.Services;
using ComputerRepairService.Services.Interfaces;
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
        private readonly IOrderManagementService _orderService;

        public ServiceOrdersController(RepairDbContext context, UserManager<ApplicationUser> userManager, IOrderManagementService orderService)
        {
            _context = context;
            _userManager = userManager;
            _orderService = orderService;
        }

        // GET: ServiceOrders
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin") || roles.Contains("Employee"))
            {
                // Админ и сотрудники видят ВСЕ заказы
                var serviceOrders = await _context.ServiceOrders.AsNoTracking()
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
                    .Include(so => so.Payments)
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
                .Include(so => so.Payments)
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();

            return View("ClientOrders", serviceOrders);
        }

        // GET: ServiceOrders/MyAssignedOrders - ТОЛЬКО для сотрудников
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyAssignedOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            var technicianIds = await _context.Technicians
               .Where(t => t.UserId == user.Id || (!string.IsNullOrWhiteSpace(user.Email) && t.Email == user.Email))
               .Select(t => t.TechnicianId)
               .ToListAsync();

            /* //Fallback для старых записей: профиль Technician мог быть создан раньше,
            // но без привязки UserId к Identity-пользователю.
            if (technician == null && !string.IsNullOrWhiteSpace(user.Email))
            {
                technician = await _context.Technicians
                    .FirstOrDefaultAsync(t => t.Email == user.Email);

                if (technician != null && string.IsNullOrWhiteSpace(technician.UserId))
                {
                    technician.UserId = user.Id;
                    await _context.SaveChangesAsync();
                }
            } 
            */

            if (!technicianIds.Any())
            {
                TempData["ErrorMessage"] = "Профиль мастера не найден. Обратитесь к администратору.";
                return RedirectToAction("Index", "Home");
            }

            // Самовосстановление связи UserId для legacy-записей Technician
            var unlinkedTechnicians = await _context.Technicians
                .Where(t => technicianIds.Contains(t.TechnicianId) && string.IsNullOrWhiteSpace(t.UserId))
                .ToListAsync();

            if (unlinkedTechnicians.Any())
            {
                foreach (var tech in unlinkedTechnicians)
                {
                    tech.UserId = user.Id;
                }
                await _context.SaveChangesAsync();
            }

            var serviceOrders = await _context.ServiceOrders
                .Where(so =>
                    so.OrderTechnicians.Any(ot => technicianIds.Contains(ot.TechnicianId))) //убрал часть кода потому что была ошибка
                .Include(so => so.Customer)
                .Include(so => so.DeviceType)
                .Include(so => so.OrderStatus)
                .Include(so => so.OrderTechnicians)
                    .ThenInclude(ot => ot.Technician)
                .Distinct()
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();

            return View(serviceOrders);
        }

        // GET: ServiceOrders/ClientRequests - входящие клиентские заявки для админов и сотрудников
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ClientRequests()
        {
            // Новые заявки клиента: статус "Новая" и без назначенных мастеров
            var requests = await _context.ServiceOrders
                .Where(so => so.StatusId == 1 && !so.OrderTechnicians.Any())
                .Include(so => so.Customer)
                .Include(so => so.DeviceType)
                .Include(so => so.OrderStatus)
                .OrderByDescending(so => so.CreatedDate)
                .ToListAsync();

            return View(requests);
        }

        // POST: ServiceOrders/TakeInWork/5 - заявка взята в обработку сотрудником/админом
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeInWork(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            bool success = await _orderService.TakeOrderInWorkAsync(id, user?.Id, User.Identity?.Name, User.IsInRole("Employee"));
            
            if (success)
            {
                TempData["SuccessMessage"] = $"Заявка #{id} взята в работу.";
            }
            else
            {
                TempData["ErrorMessage"] = "Ошибка при взятии заявки в работу.";
            }
            return RedirectToAction(nameof(ClientRequests));
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
                .Include(so => so.OrderStatusHistories)
                    .ThenInclude(h => h.OrderStatus)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (serviceOrder == null)
            {
                return NotFound();
            }

            // Проверка прав доступа
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);

            if (roles.Contains("Admin") || roles.Contains("Employee"))
            {
                // Админ и сотрудники видят все
                return View(serviceOrder);
            }
            else if (roles.Contains("Client"))
            {
                // Клиенты видят только свои заказы
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user!.Id);

                if (customer != null && serviceOrder.CustomerId == customer.CustomerId)
                {
                    ViewBag.AmountDue = OrderPaymentHelper.GetAmountDue(serviceOrder);
                    ViewBag.CanPay = OrderPaymentHelper.CanClientPay(serviceOrder);
                    ViewBag.IsFullyPaid = OrderPaymentHelper.IsFullyPaid(serviceOrder);
                    return View("ClientOrderDetails", serviceOrder);
                }
                else
                {
                    return RedirectToAction("AccessDenied", "Home");
                }
            }

            return RedirectToAction("AccessDenied", "Home");
        }
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
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadCreateViewData();
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Client"))
                {
                    var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                    if (customer != null) model.CustomerId = customer.CustomerId;
                    model.SelectedTechnicians = Array.Empty<int>();
                }

                var serviceOrder = await _orderService.CreateOrderAsync(model, user.Id, roles);
                TempData["SuccessMessage"] = "Заказ успешно создан!";
                
                if (roles.Contains("Client")) return RedirectToAction(nameof(MyOrders));
                return RedirectToAction(nameof(Details), new { id = serviceOrder.OrderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при создании заказа: {ex.Message}");
                await LoadCreateViewData();
                return View(model);
            }
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

            var model = new OrderEditViewModel
            {
                OrderId = serviceOrder.OrderId,
                CustomerId = serviceOrder.CustomerId,
                DeviceTypeId = serviceOrder.DeviceTypeId,
                DeviceBrand = serviceOrder.DeviceBrand,
                DeviceModel = serviceOrder.DeviceModel,
                SerialNumber = serviceOrder.SerialNumber,
                ProblemDescription = serviceOrder.ProblemDescription,
                DiagnosticNotes = serviceOrder.DiagnosticNotes,
                StatusId = serviceOrder.StatusId,
                Priority = serviceOrder.Priority,
                EstimatedCompletionDate = serviceOrder.EstimatedCompletionDate,
                ActualCompletionDate = serviceOrder.ActualCompletionDate,
                TechnicianNotes = serviceOrder.TechnicianNotes,
                SelectedTechnicians = serviceOrder.OrderTechnicians.Select(ot => ot.TechnicianId).ToArray()
            };

            await LoadCreateViewData();
            return View(model);
        }

        // POST: ServiceOrders/Edit/5 - ТОЛЬКО для админов и сотрудников
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderEditViewModel model)
        {
            if (id != model.OrderId || !ModelState.IsValid)
            {
                await LoadCreateViewData();
                return View(model);
            }

            try
            {
                await _orderService.EditOrderAsync(model, User.Identity?.Name);
                TempData["SuccessMessage"] = "Заказ успешно обновлен!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при обновлении заказа: {ex.Message}");
                await LoadCreateViewData();
                return View(model);
            }
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

                if (statusId == OrderStatusIds.ReadyForPickup && oldStatusId != OrderStatusIds.ReadyForPickup)
                {
                    TempData["ErrorMessage"] = "Выдать устройство можно только после подтверждения готовности.";
                }

                if (statusId == OrderStatusIds.AwaitingApproval && serviceOrder.TotalCost <= 0)
                {
                    TempData["ErrorMessage"] = "Сначала укажите стоимость через «Заказ выполнен» или редактирование заказа.";
                    return RedirectToAction(nameof(Details), new { id });
                }

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

                if (statusId == OrderStatusIds.AwaitingApproval)
                {
                    if (serviceOrder.ActualCompletionDate == null)
                    {
                        serviceOrder.ActualCompletionDate = DateTime.Now;
                    }

                    statusHistory.Notes = string.IsNullOrWhiteSpace(notes)
                        ? "Ожидается согласование клиентом."
                        : notes;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = statusId == OrderStatusIds.AwaitingApproval
                    ? "Заказ ожидает согласования клиентом."
                    : "Статус заказа успешно обновлен!";
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
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return RedirectToAction("AccessDenied", "Home");

            bool success = await _orderService.CancelOrderAsync(id, customer.CustomerId, User.Identity?.Name);
            if (success) TempData["SuccessMessage"] = "Заказ успешно отменен!";
            else TempData["ErrorMessage"] = "Ошибка при отмене заказа.";
            
            return RedirectToAction(nameof(MyOrders));
        }

        // GET: ServiceOrders/UnpaidOrders — неоплаченные заказы клиента
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> UnpaidOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user!.Id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Профиль клиента не найден.";
                return RedirectToAction("Index", "Home");
            }

            var orders = await _context.ServiceOrders
                .Where(o => o.CustomerId == customer.CustomerId && o.StatusId != OrderStatusIds.New && o.StatusId != OrderStatusIds.Cancelled)
                .Include(o => o.DeviceType)
                .Include(o => o.OrderStatus)
                .Include(o => o.Payments)
                .Include(o => o.OrderTechnicians)
                    .ThenInclude(ot => ot.Technician)
                .OrderByDescending(o => o.ActualCompletionDate ?? o.CreatedDate)
                .ToListAsync();

            orders = orders.Where(o => OrderPaymentHelper.CanClientPay(o)).ToList();
            return View(orders);
        }

        // POST: ServiceOrders/PayOrder/5 — симуляция онлайн-оплаты клиентом
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Профиль клиента не найден.";
                return RedirectToAction(nameof(UnpaidOrders));
            }

            var order = await OrderPaymentService.GetOrderForPaymentAsync(_context, id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.CustomerId != customer.CustomerId)
            {
                TempData["ErrorMessage"] = "Вы можете оплачивать только свои заказы.";
                return RedirectToAction("AccessDenied", "Home");
            }

            if (!OrderPaymentHelper.CanClientPay(order))
            {
                TempData["ErrorMessage"] = "Оплата для этого заказа сейчас недоступна.";
                return RedirectToAction(nameof(UnpaidOrders));
            }

            var amountDue = OrderPaymentHelper.GetAmountDue(order);
            var (success, error) = await OrderPaymentService.RegisterPaymentAsync(
                _context,
                order,
                amountDue,
                PaymentMethodCodes.Online,
                $"SIM-{Guid.NewGuid():N}"[..20],
                "Онлайн-оплата клиентом (симуляция)",
                User.Identity?.Name ?? customer.Email ?? "Client");

            if (!success)
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction(nameof(UnpaidOrders));
            }

            TempData["SuccessMessage"] = $"Оплата {amountDue:C} по заказу #{order.OrderId} прошла успешно. Устройство готово к получению!";
            return RedirectToAction(nameof(UnpaidOrders));
        }

        // GET: ServiceOrders/CompleteOrder/5 — мастер завершает заказ
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> CompleteOrder(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await LoadOrderForEmployeeCompletionAsync(id.Value);
            if (order == null)
            {
                return NotFound();
            }

            if (!OrderPaymentHelper.CanEmployeeComplete(order))
            {
                TempData["ErrorMessage"] = "Этот заказ нельзя завершить на текущем этапе.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new CompleteOrderViewModel
            {
                OrderId = order.OrderId,
                CustomerName = order.Customer != null
                    ? $"{order.Customer.FirstName} {order.Customer.LastName}"
                    : "—",
                DeviceDescription = $"{order.DeviceType?.TypeName} {order.DeviceBrand} {order.DeviceModel}".Trim(),
                ProblemDescription = order.ProblemDescription,
                CurrentStatusName = order.OrderStatus?.StatusName ?? "—",
                TotalCost = order.TotalCost > 0 ? order.TotalCost : 0,
                TechnicianNotes = order.TechnicianNotes
            };

            return View(model);
        }

        // POST: ServiceOrders/CompleteOrder/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id, CompleteOrderViewModel model)
        {
            if (id != model.OrderId)
            {
                return NotFound();
            }

            var order = await LoadOrderForEmployeeCompletionAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (!OrderPaymentHelper.CanEmployeeComplete(order))
            {
                TempData["ErrorMessage"] = "Этот заказ нельзя завершить на текущем этапе.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!ModelState.IsValid)
            {
                model.CustomerName = order.Customer != null
                    ? $"{order.Customer.FirstName} {order.Customer.LastName}"
                    : "—";
                model.DeviceDescription = $"{order.DeviceType?.TypeName} {order.DeviceBrand} {order.DeviceModel}".Trim();
                model.ProblemDescription = order.ProblemDescription;
                model.CurrentStatusName = order.OrderStatus?.StatusName ?? "—";
                return View(model);
            }

            order.TotalCost = model.TotalCost;
            order.TechnicianNotes = model.TechnicianNotes;
            order.ActualCompletionDate = DateTime.Now;

            order.StatusId = OrderStatusIds.ReadyForPickup;

            string statusNote = model.TotalCost > 0 
                ? $"Работы завершены. Стоимость: {model.TotalCost:C}. Ожидается оплата и выдача клиенту."
                : "Работы завершены (бесплатно/по гарантии). Заказ готов к выдаче клиенту.";

            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = OrderStatusIds.ReadyForPickup,
                ChangedDate = DateTime.Now,
                ChangedBy = User.Identity?.Name ?? "Employee",
                Notes = statusNote
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Заказ #{order.OrderId} завершён. Готов к выдаче.";
            
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: ServiceOrders/IssueOrder/5
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> IssueOrder(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.ServiceOrders
                .Include(o => o.Customer)
                .Include(o => o.DeviceType)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            var amountDue = OrderPaymentHelper.GetAmountDue(order);

            var model = new IssueOrderViewModel
            {
                OrderId = order.OrderId,
                CustomerName = order.Customer != null ? $"{order.Customer.FirstName} {order.Customer.LastName}" : "—",
                DeviceDescription = $"{order.DeviceType?.TypeName} {order.DeviceBrand} {order.DeviceModel}".Trim(),
                TotalCost = order.TotalCost,
                PaidAmount = OrderPaymentHelper.GetPaidAmount(order),
                AmountDue = amountDue,
                PaymentMethod = amountDue > 0 ? "Наличные" : "Не требуется"
            };

            return View(model);
        }

        // POST: ServiceOrders/IssueOrder/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueOrder(int id, IssueOrderViewModel model)
        {
            if (id != model.OrderId) return NotFound();

            var order = await _context.ServiceOrders.Include(o => o.Payments).FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();

            var amountDue = OrderPaymentHelper.GetAmountDue(order);

            if (amountDue > 0 && model.PaymentMethod != "Не требуется")
            {
                // Регистрируем платеж
                var (success, error) = await OrderPaymentService.RegisterPaymentAsync(
                    _context,
                    order,
                    amountDue,
                    model.PaymentMethod,
                    $"POS-{Guid.NewGuid():N}"[..12],
                    "Оплата при выдаче",
                    User.Identity?.Name ?? "Employee");

                if (!success)
                {
                    TempData["ErrorMessage"] = "Ошибка при регистрации оплаты: " + error;
                    return View(model);
                }
            }
            else if (amountDue > 0)
            {
                TempData["ErrorMessage"] = "Необходимо выбрать способ оплаты для погашения остатка.";
                return View(model);
            }

            order.StatusId = OrderStatusIds.Issued;
            order.ActualCompletionDate = order.ActualCompletionDate ?? DateTime.Now;
            
            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = OrderStatusIds.Issued,
                ChangedDate = DateTime.Now,
                ChangedBy = User.Identity?.Name ?? "Employee",
                Notes = "Устройство выдано клиенту. Заказ закрыт."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Заказ #{id} успешно закрыт и выдан.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: ServiceOrders/RequireApproval/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequireApproval(int id, decimal estimatedCost, string notes)
        {
            var order = await _context.ServiceOrders.FindAsync(id);
            if (order == null) return NotFound();

            if (!OrderStatusIds.ActiveWorkStatuses.Contains(order.StatusId) && order.StatusId != OrderStatusIds.New)
            {
                TempData["ErrorMessage"] = "Перевести в «Требует согласования» можно только активный или новый заказ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.TotalCost = estimatedCost; // Временно сохраняем как TotalCost, это будет сумма к согласованию
            order.StatusId = OrderStatusIds.AwaitingApproval;

            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = OrderStatusIds.AwaitingApproval,
                ChangedDate = DateTime.Now,
                ChangedBy = User.Identity?.Name ?? "Employee",
                Notes = $"Требуется согласование. Предварительная стоимость: {estimatedCost:C}. Заметки: {notes}"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Заказ #{id} переведен в статус «Требует согласования».";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: ServiceOrders/ApproveOrder/5
        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user!.Id);
            if (customer == null) return NotFound();

            var order = await _context.ServiceOrders.FindAsync(id);
            if (order == null || order.CustomerId != customer.CustomerId) return NotFound();

            if (order.StatusId != OrderStatusIds.AwaitingApproval)
            {
                TempData["ErrorMessage"] = "Этот заказ не требует согласования.";
                return RedirectToAction(nameof(Details), new { id });
            }

            order.StatusId = OrderStatusIds.InRepair;

            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = OrderStatusIds.InRepair,
                ChangedDate = DateTime.Now,
                ChangedBy = User.Identity?.Name ?? "Client",
                Notes = $"Клиент подтвердил ремонт и стоимость ({order.TotalCost:C})."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Вы успешно подтвердили заказ #{id}. Мы начинаем ремонт!";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<ServiceOrder?> LoadOrderForEmployeeCompletionAsync(int orderId)
        {
            var order = await _context.ServiceOrders
                .Include(o => o.Customer)
                .Include(o => o.DeviceType)
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderTechnicians)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return null;
            }

            if (User.IsInRole("Employee"))
            {
                var user = await _userManager.GetUserAsync(User);
                var technicianIds = await _context.Technicians
                    .Where(t => t.UserId == user!.Id || (!string.IsNullOrWhiteSpace(user.Email) && t.Email == user.Email))
                    .Select(t => t.TechnicianId)
                    .ToListAsync();

                if (!order.OrderTechnicians.Any(ot => technicianIds.Contains(ot.TechnicianId)))
                {
                    return null;
                }
            }

            return order;
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

        private async Task<int?> GetStatusIdByNameAsync(string statusName)
        {
            var normalized = statusName.Trim().ToLower();
            return await _context.OrderStatuses
                .Where(s => s.StatusName.ToLower() == normalized)
                .Select(s => (int?)s.StatusId)
                .FirstOrDefaultAsync();
        }
    }
}