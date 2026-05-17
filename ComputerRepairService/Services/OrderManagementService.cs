using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.Enums;
using ComputerRepairService.Models.ViewModels;
using ComputerRepairService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Services
{
    public class OrderManagementService : IOrderManagementService
    {
        private readonly RepairDbContext _context;

        public OrderManagementService(RepairDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceOrder> CreateOrderAsync(OrderCreateViewModel model, string? currentUserId, IList<string> userRoles)
        {
            var serviceOrder = new ServiceOrder
            {
                CustomerId = model.CustomerId,
                DeviceTypeId = model.DeviceTypeId,
                DeviceBrand = model.DeviceBrand,
                DeviceModel = model.DeviceModel,
                SerialNumber = string.IsNullOrWhiteSpace(model.SerialNumber) 
                    ? "SN-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper() 
                    : model.SerialNumber,
                ProblemDescription = model.ProblemDescription,
                Priority = model.Priority > 0 ? model.Priority : 2,
                CreatedDate = DateTime.Now,
                StatusId = 1 // Новая
            };

            if (userRoles.Contains("Admin") || userRoles.Contains("Employee"))
            {
                serviceOrder.DiagnosticNotes = model.DiagnosticNotes;
                serviceOrder.TechnicianNotes = model.TechnicianNotes;
            }

            _context.ServiceOrders.Add(serviceOrder);
            await _context.SaveChangesAsync(); // Сохраняем, чтобы получить OrderId

            // Добавляем мастеров, если это админ/сотрудник и мастера выбраны
            if ((userRoles.Contains("Admin") || userRoles.Contains("Employee")) && model.SelectedTechnicians != null && model.SelectedTechnicians.Any())
            {
                foreach (var techId in model.SelectedTechnicians)
                {
                    _context.OrderTechnicians.Add(new OrderTechnician
                    {
                        OrderId = serviceOrder.OrderId,
                        TechnicianId = techId,
                        AssignedDate = DateTime.Now,
                        IsPrimary = true
                    });
                }
            }

            // История
            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = serviceOrder.OrderId,
                StatusId = serviceOrder.StatusId,
                ChangedDate = DateTime.Now,
                ChangedBy = "System", // Можно улучшить передачей UserName
                Notes = userRoles.Contains("Client") ? "Заказ создан клиентом" : "Заказ создан"
            });

            await _context.SaveChangesAsync();
            return serviceOrder;
        }

        public async Task<ServiceOrder> EditOrderAsync(OrderEditViewModel model, string? currentUserName)
        {
            var existingOrder = await _context.ServiceOrders
                .Include(so => so.OrderTechnicians)
                .FirstOrDefaultAsync(so => so.OrderId == model.OrderId);

            if (existingOrder == null)
                throw new Exception("Заказ не найден");

            var previousStatusId = existingOrder.StatusId;

            existingOrder.CustomerId = model.CustomerId;
            existingOrder.DeviceTypeId = model.DeviceTypeId;
            existingOrder.DeviceBrand = model.DeviceBrand;
            existingOrder.DeviceModel = model.DeviceModel;
            existingOrder.SerialNumber = model.SerialNumber;
            existingOrder.ProblemDescription = model.ProblemDescription;
            existingOrder.DiagnosticNotes = model.DiagnosticNotes;
            existingOrder.StatusId = model.StatusId;
            existingOrder.Priority = model.Priority;
            existingOrder.EstimatedCompletionDate = model.EstimatedCompletionDate;
            existingOrder.TechnicianNotes = model.TechnicianNotes;

            if (model.StatusId == OrderStatusIds.AwaitingApproval && existingOrder.ActualCompletionDate == null)
            {
                existingOrder.ActualCompletionDate = DateTime.Now;
            }

            _context.OrderTechnicians.RemoveRange(existingOrder.OrderTechnicians);

            if (model.SelectedTechnicians != null && model.SelectedTechnicians.Any())
            {
                foreach (var techId in model.SelectedTechnicians)
                {
                    _context.OrderTechnicians.Add(new OrderTechnician
                    {
                        OrderId = existingOrder.OrderId,
                        TechnicianId = techId,
                        AssignedDate = DateTime.Now,
                        IsPrimary = true
                    });
                }
            }

            if (previousStatusId != model.StatusId)
            {
                var historyNotes = "Статус изменен при редактировании заказа";
                if (model.StatusId == OrderStatusIds.AwaitingApproval)
                    historyNotes = "Заказ переведён в ожидание согласования.";

                _context.OrderStatusHistory.Add(new OrderStatusHistory
                {
                    OrderId = existingOrder.OrderId,
                    StatusId = model.StatusId,
                    ChangedDate = DateTime.Now,
                    ChangedBy = currentUserName ?? "System",
                    Notes = historyNotes
                });
            }

            await _context.SaveChangesAsync();
            return existingOrder;
        }

        public async Task<bool> TakeOrderInWorkAsync(int orderId, string? userId, string? userName, bool isEmployee)
        {
            var order = await _context.ServiceOrders
                .Include(so => so.OrderTechnicians)
                .FirstOrDefaultAsync(so => so.OrderId == orderId);

            if (order == null || order.StatusId is 5 or 6 or 7)
                return false;

            var diagnosisStatusId = await _context.OrderStatuses
                .Where(s => s.StatusName.ToLower() == "диагностика")
                .Select(s => (int?)s.StatusId)
                .FirstOrDefaultAsync();

            if (diagnosisStatusId.HasValue)
                order.StatusId = diagnosisStatusId.Value;

            var note = "Заявка взята в работу";

            if (isEmployee && !string.IsNullOrEmpty(userId))
            {
                var technician = await _context.Technicians
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (technician != null && !order.OrderTechnicians.Any(ot => ot.TechnicianId == technician.TechnicianId))
                {
                    _context.OrderTechnicians.Add(new OrderTechnician
                    {
                        OrderId = order.OrderId,
                        TechnicianId = technician.TechnicianId,
                        AssignedDate = DateTime.Now,
                        IsPrimary = true
                    });
                    note += $"; назначен мастер {technician.FirstName} {technician.LastName}";
                }
            }

            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = order.StatusId,
                ChangedDate = DateTime.Now,
                ChangedBy = userName ?? "System",
                Notes = note
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, int customerId, string? userName)
        {
            var order = await _context.ServiceOrders.FindAsync(orderId);
            if (order == null || order.CustomerId != customerId) return false;
            
            if (order.StatusId != 1 && order.StatusId != 2) return false;

            order.StatusId = 7; // Отменено
            
            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                StatusId = 7,
                ChangedDate = DateTime.Now,
                ChangedBy = userName ?? "System",
                Notes = "Заказ отменен клиентом"
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
