using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.Enums;
using ComputerRepairService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Services
{
    public class OrderPartsService : IOrderPartsService
    {
        private readonly RepairDbContext _context;

        public OrderPartsService(RepairDbContext context)
        {
            _context = context;
        }

        public bool CanManageParts(ServiceOrder order)
        {
            return OrderStatusIds.ActiveWorkStatuses.Contains(order.StatusId);
        }

        public decimal GetPartsTotal(IEnumerable<OrderPart> parts)
        {
            return parts.Sum(p => p.QuantityUsed * p.UnitPriceAtTime);
        }

        public async Task<ServiceOrder?> GetOrderForPartsManagementAsync(
            int orderId, string? userId, bool isAdmin, bool isEmployee)
        {
            var order = await _context.ServiceOrders
                .Include(o => o.OrderTechnicians)
                .Include(o => o.OrderParts)
                    .ThenInclude(op => op.Inventory)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return null;
            }

            if (isEmployee && !isAdmin)
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                var technicianIds = await _context.Technicians
                    .Where(t => t.UserId == userId)
                    .Select(t => t.TechnicianId)
                    .ToListAsync();

                if (!order.OrderTechnicians.Any(ot => technicianIds.Contains(ot.TechnicianId)))
                {
                    return null;
                }
            }

            return order;
        }

        public async Task<(bool Success, string? Error, string? Warning)> AddPartToOrderAsync(
            int orderId, int partId, int quantity, string changedBy,
            string? userId, bool isAdmin, bool isEmployee)
        {
            if (quantity <= 0)
            {
                return (false, "Укажите количество больше нуля.", null);
            }

            var order = await GetOrderForPartsManagementAsync(orderId, userId, isAdmin, isEmployee);
            if (order == null)
            {
                return (false, "Заказ не найден или у вас нет доступа.", null);
            }

            if (!CanManageParts(order))
            {
                return (false, "Списывать запчасти можно только на этапе диагностики, ожидания запчастей или ремонта.", null);
            }

            var part = await _context.Inventory.FirstOrDefaultAsync(i => i.PartId == partId && i.IsActive);
            if (part == null)
            {
                return (false, "Запчасть не найдена или снята с учёта.", null);
            }

            if (part.QuantityInStock < quantity)
            {
                return (false,
                    $"Недостаточно «{part.PartName}» на складе: доступно {part.QuantityInStock} шт., запрошено {quantity}. " +
                    "Пополните склад или переведите заказ в статус «Ожидание запчастей».",
                    null);
            }

            var orderPart = new OrderPart
            {
                OrderId = orderId,
                PartId = partId,
                QuantityUsed = quantity,
                UnitPriceAtTime = part.UnitPrice,
                UsageDate = DateTime.Now
            };

            _context.OrderParts.Add(orderPart);
            part.QuantityInStock -= quantity;

            var historyNote =
                $"Списано со склада: {part.PartName} — {quantity} шт. × {part.UnitPrice:C} " +
                $"(остаток: {part.QuantityInStock} шт.)";

            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                StatusId = order.StatusId,
                ChangedDate = DateTime.Now,
                ChangedBy = changedBy,
                Notes = historyNote
            });

            await _context.SaveChangesAsync();

            string? warning = null;
            if (part.QuantityInStock <= part.ReorderLevel)
            {
                warning = part.QuantityInStock == 0
                    ? $"«{part.PartName}» закончилась на складе. Рекомендуется пополнение."
                    : $"«{part.PartName}» — критический остаток ({part.QuantityInStock} шт., порог {part.ReorderLevel}).";
            }

            return (true, null, warning);
        }

        public async Task<(bool Success, string? Error)> RemovePartFromOrderAsync(
            int orderPartId, string changedBy, string? userId, bool isAdmin, bool isEmployee)
        {
            var orderPart = await _context.OrderParts
                .Include(op => op.Inventory)
                .Include(op => op.ServiceOrder)
                    .ThenInclude(so => so.OrderTechnicians)
                .FirstOrDefaultAsync(op => op.OrderPartId == orderPartId);

            if (orderPart == null)
            {
                return (false, "Запись о запчасти не найдена.");
            }

            var order = orderPart.ServiceOrder;
            if (order == null)
            {
                return (false, "Заказ не найден.");
            }

            if (isEmployee && !isAdmin)
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return (false, "Недостаточно прав.");
                }

                var technicianIds = await _context.Technicians
                    .Where(t => t.UserId == userId)
                    .Select(t => t.TechnicianId)
                    .ToListAsync();

                if (!order.OrderTechnicians.Any(ot => technicianIds.Contains(ot.TechnicianId)))
                {
                    return (false, "Вы не назначены на этот заказ.");
                }
            }

            if (!CanManageParts(order))
            {
                return (false, "Удалять запчасти из заказа можно только пока ремонт в работе.");
            }

            var partName = orderPart.Inventory?.PartName ?? $"#{orderPart.PartId}";
            orderPart.Inventory!.QuantityInStock += orderPart.QuantityUsed;

            _context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = order.StatusId,
                ChangedDate = DateTime.Now,
                ChangedBy = changedBy,
                Notes =
                    $"Возврат на склад: {partName} — {orderPart.QuantityUsed} шт. " +
                    $"(остаток: {orderPart.Inventory.QuantityInStock} шт.)"
            });

            _context.OrderParts.Remove(orderPart);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task RestoreStockForOrderAsync(int orderId)
        {
            var orderParts = await _context.OrderParts
                .Include(op => op.Inventory)
                .Where(op => op.OrderId == orderId)
                .ToListAsync();

            foreach (var orderPart in orderParts)
            {
                if (orderPart.Inventory != null)
                {
                    orderPart.Inventory.QuantityInStock += orderPart.QuantityUsed;
                }
            }

            if (orderParts.Count > 0)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
