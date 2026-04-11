using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.Enums;
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
            return View(payments);
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

        // GET: Payments/Create
        public async Task<IActionResult> Create(int? orderId)
        {
            await PopulateServiceOrdersAsync(orderId);
            return View();
        }

        // POST: Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("OrderId,Amount,PaymentMethod,Status,TransactionId,Notes")] Payment payment)
        {
            ModelState.Remove(nameof(Payment.ServiceOrder));

            if (payment.OrderId <= 0)
            {
                ModelState.AddModelError(nameof(Payment.OrderId), "Выберите заказ");
            }
            else if (!await _context.ServiceOrders.AnyAsync(so => so.OrderId == payment.OrderId))
            {
                ModelState.AddModelError(nameof(Payment.OrderId), "Указанный заказ не найден");
            }

            if (!string.IsNullOrEmpty(payment.PaymentMethod) &&
                !PaymentMethodCodes.All.Contains(payment.PaymentMethod))
            {
                ModelState.AddModelError(nameof(Payment.PaymentMethod), "Недопустимый метод оплаты");
            }

            if (string.IsNullOrWhiteSpace(payment.Status))
            {
                payment.Status = "Completed";
            }

            ValidateAmount(payment.Amount, ModelState);

            if (ModelState.IsValid)
            {
                payment.PaymentDate = DateTime.Now;
                _context.Add(payment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Платёж сохранён.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateServiceOrdersAsync(payment.OrderId);
            return View(payment);
        }

        // GET: Payments/Edit/5
        [Authorize(Roles = "Admin,Employee")]
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

        // POST: Payments/Edit/5
        [Authorize(Roles = "Admin,Employee")]
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

        private async Task PopulateServiceOrdersAsync(int? preferredOrderId)
        {
            var query = _context.ServiceOrders.AsQueryable();
            if (preferredOrderId.HasValue)
            {
                query = query.Where(so => so.OrderId == preferredOrderId.Value);
            }

            ViewBag.OrderId = preferredOrderId;
            ViewBag.ServiceOrders = await query
                .Include(so => so.Customer)
                .OrderByDescending(so => so.OrderId)
                .ToListAsync();
        }
    }
}
