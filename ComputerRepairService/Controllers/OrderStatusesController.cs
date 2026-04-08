using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.Controllers
{
    public class OrderStatusesController : Controller
    {
        private readonly RepairDbContext _context;

        public OrderStatusesController(RepairDbContext context)
        {
            _context = context;
        }

        // GET: OrderStatuses
        public async Task<IActionResult> Index()
        {
            return View(await _context.OrderStatuses.ToListAsync());
        }

        // GET: OrderStatuses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(m => m.StatusId == id);
            if (orderStatus == null)
            {
                return NotFound();
            }

            return View(orderStatus);
        }

        // GET: OrderStatuses/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: OrderStatuses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderStatus orderStatus)
        {
            if (ModelState.IsValid)
            {
                _context.Add(orderStatus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(orderStatus);
        }

        // GET: OrderStatuses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderStatus = await _context.OrderStatuses.FindAsync(id);
            if (orderStatus == null)
            {
                return NotFound();
            }
            return View(orderStatus);
        }

        // POST: OrderStatuses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrderStatus orderStatus)
        {
            if (id != orderStatus.StatusId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderStatus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderStatusExists(orderStatus.StatusId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(orderStatus);
        }

        // GET: OrderStatuses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(m => m.StatusId == id);
            if (orderStatus == null)
            {
                return NotFound();
            }

            return View(orderStatus);
        }

        // POST: OrderStatuses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var orderStatus = await _context.OrderStatuses.FindAsync(id);
            if (orderStatus != null)
            {
                _context.OrderStatuses.Remove(orderStatus);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderStatusExists(int id)
        {
            return _context.OrderStatuses.Any(e => e.StatusId == id);
        }
    }
}