using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class InventoryController : Controller
    {
        private readonly RepairDbContext _context;

        public InventoryController(RepairDbContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index()
        {
            var inventory = await _context.Inventory
                .Include(i => i.PartCategory)
                .Where(i => i.IsActive)
                .ToListAsync();
            return View(inventory);
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventory
                .Include(i => i.PartCategory)
                .Include(i => i.OrderParts)
                    .ThenInclude(op => op.ServiceOrder)
                .FirstOrDefaultAsync(m => m.PartId == id);

            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // GET: Inventory/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["CategoryId"] = _context.PartCategories.ToList();
            return View();
        }

        // POST: Inventory/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inventory inventory)
        {
            if (ModelState.IsValid)
            {
                inventory.IsActive = true;
                _context.Add(inventory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = _context.PartCategories.ToList();
            return View(inventory);
        }

        // GET: Inventory/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = _context.PartCategories.ToList();
            return View(inventory);
        }

        // POST: Inventory/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Inventory inventory)
        {
            if (id != inventory.PartId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(inventory.PartId))
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
            ViewData["CategoryId"] = _context.PartCategories.ToList();
            return View(inventory);
        }

        // GET: Inventory/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventory
                .Include(i => i.PartCategory)
                .FirstOrDefaultAsync(m => m.PartId == id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // POST: Inventory/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory != null)
            {
                inventory.IsActive = false;
                _context.Update(inventory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventory/LowStock
        public async Task<IActionResult> LowStock()
        {
            var lowStockItems = await _context.Inventory
                .Include(i => i.PartCategory)
                .Where(i => i.QuantityInStock <= i.ReorderLevel && i.IsActive)
                .ToListAsync();
            return View(lowStockItems);
        }

        // GET: Inventory/Restock/5
        public async Task<IActionResult> Restock(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // POST: Inventory/Restock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(int id, int quantity)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory == null)
            {
                return NotFound();
            }

            if (quantity > 0)
            {
                inventory.QuantityInStock += quantity;
                _context.Update(inventory);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventory.Any(e => e.PartId == id);
        }
    }
}