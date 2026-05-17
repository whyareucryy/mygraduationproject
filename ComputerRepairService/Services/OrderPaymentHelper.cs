using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.Enums;

namespace ComputerRepairService.Services
{
    public static class OrderPaymentHelper
    {
        public static decimal GetPaidAmount(ServiceOrder order)
        {
            if (order.Payments == null || !order.Payments.Any())
            {
                return 0;
            }

            return order.Payments
                .Where(p => p.Status == PaymentStatusCodes.Completed)
                .Sum(p => p.Amount);
        }

        public static decimal GetAmountDue(ServiceOrder order)
        {
            if (order.TotalCost <= 0)
            {
                return 0;
            }

            return Math.Max(0, order.TotalCost - GetPaidAmount(order));
        }

        public static bool IsAwaitingPayment(ServiceOrder order)
        {
            return order.StatusId == OrderStatusIds.ReadyForPickup
                && order.TotalCost > 0
                && GetAmountDue(order) > 0;
        }

        public static bool IsFullyPaid(ServiceOrder order)
        {
            return order.TotalCost > 0 && GetAmountDue(order) <= 0;
        }

        public static bool CanClientPay(ServiceOrder order) 
        {
            // Клиент может платить, если есть неоплаченный остаток и заказ не отменен/новый
            return GetAmountDue(order) > 0 && order.StatusId != OrderStatusIds.New && order.StatusId != OrderStatusIds.Cancelled;
        }

        public static bool CanEmployeeComplete(ServiceOrder order)
        {
            return OrderStatusIds.ActiveWorkStatuses.Contains(order.StatusId);
        }

        public static bool CanClientApprove(ServiceOrder order)
        {
            return order.StatusId == OrderStatusIds.AwaitingApproval;
        }

        public static bool CanIssueDevice(ServiceOrder order)
        {
            // Мы можем перейти к выдаче устройства если статус "Готово к выдаче" ИЛИ "В ремонте".
            // Оплата (если есть долг) будет принята прямо на странице выдачи.
            return order.StatusId == OrderStatusIds.ReadyForPickup || order.StatusId == OrderStatusIds.InRepair;
        }
    }
}
