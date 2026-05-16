using ComputerRepairService.Data;
using ComputerRepairService.Models.Enums;
using ComputerRepairService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.ViewComponents
{
    public class PaymentNotificationsViewComponent : ViewComponent
    {
        private readonly RepairDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentNotificationsViewComponent(RepairDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new PaymentNotificationsViewModel();

            if (!HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return View(model);
            }

            if (HttpContext.User.IsInRole("Client"))
            {
                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer != null)
                {
                    var orders = await _context.ServiceOrders
                        .AsNoTracking()
                        .Include(o => o.Payments)
                        .Where(o => o.CustomerId == customer.CustomerId
                            && o.StatusId == OrderStatusIds.AwaitingPayment)
                        .OrderByDescending(o => o.ActualCompletionDate ?? o.CreatedDate)
                        .Take(10)
                        .ToListAsync();

                    foreach (var order in orders.Where(OrderPaymentHelper.CanClientPay))
                    {
                        model.Items.Add(new PaymentNotificationItem
                        {
                            IconClass = "fas fa-credit-card text-primary",
                            Message = $"Заказ #{order.OrderId} — оплатите {OrderPaymentHelper.GetAmountDue(order):C}",
                            Url = Url.Action("UnpaidOrders", "ServiceOrders") ?? "#",
                            TimeLabel = order.ActualCompletionDate?.ToString("dd.MM.yyyy") ?? "к оплате"
                        });
                    }
                }
            }
            else if (HttpContext.User.IsInRole("Admin") || HttpContext.User.IsInRole("Employee"))
            {
                var since = DateTime.Now.AddDays(-7);
                var recentPayments = await _context.Payments
                    .AsNoTracking()
                    .Include(p => p.ServiceOrder)
                        .ThenInclude(so => so!.Customer)
                    .Where(p => p.PaymentDate >= since
                        && p.Status == PaymentStatusCodes.Completed)
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(8)
                    .ToListAsync();

                foreach (var payment in recentPayments)
                {
                    var customerName = payment.ServiceOrder?.Customer != null
                        ? $"{payment.ServiceOrder.Customer.FirstName} {payment.ServiceOrder.Customer.LastName}"
                        : "Клиент";

                    model.Items.Add(new PaymentNotificationItem
                    {
                        IconClass = "fas fa-check-circle text-success",
                        Message = $"{customerName} — заказ #{payment.OrderId}, {payment.Amount:C}",
                        Url = Url.Action("Details", "Payments", new { id = payment.PaymentId }) ?? "#",
                        TimeLabel = payment.PaymentDate.ToString("dd.MM.yyyy HH:mm")
                    });
                }

                var awaitingCount = await _context.ServiceOrders
                    .AsNoTracking()
                    .CountAsync(o => o.StatusId == OrderStatusIds.AwaitingPayment);

                if (awaitingCount > 0 && model.Items.Count < 6)
                {
                    model.Items.Insert(0, new PaymentNotificationItem
                    {
                        IconClass = "fas fa-hourglass-half text-warning",
                        Message = $"{awaitingCount} заказ(ов) ожидают оплаты",
                        Url = Url.Action("Index", "Payments") ?? "#",
                        TimeLabel = "сейчас"
                    });
                }
            }

            return View(model);
        }
    }

    public class PaymentNotificationsViewModel
    {
        public List<PaymentNotificationItem> Items { get; set; } = new();
    }

    public class PaymentNotificationItem
    {
        public string IconClass { get; set; } = "fas fa-info-circle text-muted";
        public string Message { get; set; } = string.Empty;
        public string Url { get; set; } = "#";
        public string TimeLabel { get; set; } = string.Empty;
    }
}
