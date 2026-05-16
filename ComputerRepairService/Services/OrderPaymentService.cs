using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Services
{
    public static class OrderPaymentService
    {
        public static async Task<(bool Success, string? ErrorMessage)> RegisterPaymentAsync(
            RepairDbContext context,
            ServiceOrder order,
            decimal amount,
            string paymentMethod,
            string? transactionId,
            string? notes,
            string changedBy)
        {
            if (order.StatusId != OrderStatusIds.AwaitingPayment)
            {
                return (false, "Платёж можно провести только для заказов в статусе «Ожидание оплаты».");
            }

            if (amount < 0.01m)
            {
                return (false, "Сумма платежа должна быть больше нуля.");
            }

            var due = OrderPaymentHelper.GetAmountDue(order);
            if (due > 0 && amount > due)
            {
                return (false, $"Сумма превышает остаток к оплате ({due:C}).");
            }

            var payment = new Payment
            {
                OrderId = order.OrderId,
                Amount = amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                Status = PaymentStatusCodes.Completed,
                TransactionId = transactionId,
                Notes = notes
            };

            context.Payments.Add(payment);

            context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = OrderStatusIds.AwaitingPayment,
                ChangedDate = DateTime.Now,
                ChangedBy = changedBy,
                Notes = $"Получена оплата {amount:C} ({paymentMethod})"
            });

            order.StatusId = OrderStatusIds.ReadyForPickup;

            context.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = order.OrderId,
                StatusId = OrderStatusIds.ReadyForPickup,
                ChangedDate = DateTime.Now,
                ChangedBy = changedBy,
                Notes = "Оплата подтверждена. Заказ готов к выдаче клиенту."
            });

            await context.SaveChangesAsync();
            return (true, null);
        }

        public static async Task<ServiceOrder?> GetOrderForPaymentAsync(RepairDbContext context, int orderId)
        {
            return await context.ServiceOrders
                .Include(o => o.Payments)
                .Include(o => o.OrderStatus)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }
    }
}
