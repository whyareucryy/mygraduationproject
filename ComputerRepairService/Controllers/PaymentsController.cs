using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.Enums;
using ComputerRepairService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ComputerRepairService.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class PaymentsController : Controller
    {
        private const decimal MinAmount = 0.01m;
        private const decimal MaxAmount = 999999.99m;

        private readonly RepairDbContext _context;

        public PaymentsController(RepairDbContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Payments
                .Include(p => p.ServiceOrder)
                    .ThenInclude(so => so!.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.StatusFilter = status ?? string.Empty;
            ViewBag.OrdersAwaitingPayment = await GetOrdersAwaitingPaymentAsync();
            return View(payments);
        }

        // POST: Payments/ConfirmPayment/5 — подтверждение оплаты по заказу
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int orderId, string paymentMethod, string? notes)
        {
            var order = await OrderPaymentService.GetOrderForPaymentAsync(_context, orderId);
            if (order == null)
            {
                return NotFound();
            }

            if (!OrderPaymentHelper.CanClientPay(order))
            {
                TempData["ErrorMessage"] = "Для этого заказа оплата недоступна.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(paymentMethod) || !PaymentMethodCodes.All.Contains(paymentMethod))
            {
                TempData["ErrorMessage"] = "Выберите корректный метод оплаты.";
                return RedirectToAction("IssueOrder", "ServiceOrders", new { id = orderId });
            }

            var amountDue = OrderPaymentHelper.GetAmountDue(order);
            if (amountDue <= 0)
            {
                TempData["ErrorMessage"] = "По этому заказу нечего оплачивать.";
                return RedirectToAction(nameof(Index));
            }

            var (success, error) = await OrderPaymentService.RegisterPaymentAsync(
                _context,
                order,
                amountDue,
                paymentMethod,
                null,
                notes ?? "Оплата подтверждена сотрудником",
                User.Identity?.Name ?? "Staff");

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
                ? $"Оплата по заказу #{orderId} подтверждена."
                : error;

            return RedirectToAction(nameof(Index));
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.ServiceOrder)
                    .ThenInclude(so => so!.Customer)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            await PopulateServiceOrdersAsync(payment.OrderId);
            return View(payment);
        }

        // POST: Payments/Edit/5 — только существующие платежи (без смены заказа на неподходящий)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("PaymentId,OrderId,Amount,PaymentMethod,Status,TransactionId,Notes,PaymentDate")] Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Payment.ServiceOrder));

            var existing = await _context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.PaymentId == id);
            if (existing != null && existing.OrderId != payment.OrderId)
            {
                var newOrder = await _context.ServiceOrders.FindAsync(payment.OrderId);
                if (newOrder?.StatusId != OrderStatusIds.ReadyForPickup)
                {
                    ModelState.AddModelError(nameof(Payment.OrderId),
                        "Нельзя привязать платёж к заказу не в статусе «Ожидание оплаты».");
                }
            }

            if (!string.IsNullOrEmpty(payment.PaymentMethod) &&
                !PaymentMethodCodes.All.Contains(payment.PaymentMethod))
            {
                ModelState.AddModelError(nameof(Payment.PaymentMethod), "Недопустимый метод оплаты");
            }

            ValidateAmount(payment.Amount, ModelState);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.PaymentId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                TempData["SuccessMessage"] = "Платёж обновлён.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateServiceOrdersAsync(payment.OrderId);
            return View(payment);
        }

        // GET: Payments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.ServiceOrder)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Платёж удалён.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }

        private static void ValidateAmount(decimal amount, ModelStateDictionary modelState)
        {
            if (amount < MinAmount)
            {
                modelState.AddModelError(nameof(Payment.Amount), "Сумма должна быть больше нуля.");
            }

            if (amount > MaxAmount)
            {
                modelState.AddModelError(nameof(Payment.Amount), "Сумма слишком велика (максимум 999 999,99).");
            }
        }

        private async Task<List<ServiceOrder>> GetOrdersAwaitingPaymentAsync()
        {
            var orders = await _context.ServiceOrders
                .Where(so => so.StatusId != OrderStatusIds.New && so.StatusId != OrderStatusIds.Cancelled)
                .Include(so => so.Customer)
                .Include(so => so.OrderStatus)
                .Include(so => so.Payments)
                .OrderByDescending(so => so.ActualCompletionDate ?? so.CreatedDate)
                .ToListAsync();

            // Оставляем только те, по которым реально есть остаток к оплате (TotalCost > PaidAmount)
            return orders.Where(so => OrderPaymentHelper.GetAmountDue(so) > 0).ToList();
        }

        private async Task PopulateServiceOrdersAsync(int? preferredOrderId)
        {
            var query = _context.ServiceOrders
                .Where(so => so.StatusId != OrderStatusIds.New && so.StatusId != OrderStatusIds.Cancelled);

            if (preferredOrderId.HasValue)
            {
                query = query.Where(so => so.OrderId == preferredOrderId.Value);
            }

            var orders = await query
                .Include(so => so.Customer)
                .Include(so => so.Payments)
                .OrderByDescending(so => so.OrderId)
                .ToListAsync();

            ViewBag.OrderId = preferredOrderId;
            ViewBag.ServiceOrders = orders.Where(so => OrderPaymentHelper.GetAmountDue(so) > 0).ToList();
        }
    }
}
