using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        [Authorize(Roles = "Admin,Employee")]
        // GET: Inventory
        public async Task<IActionResult> Index()
        {
            var inventory = await _context.Inventory
                .Include(i => i.PartCategory)
                .Where(i => i.IsActive)
                .OrderBy(i => i.QuantityInStock <= i.ReorderLevel)
                .ThenBy(i => i.PartName)
                .ToListAsync();

            return View(inventory);
        }

        [Authorize(Roles = "Admin,Employee")]
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
                .FirstOrDefaultAsync(m => m.PartId == id && m.IsActive);

            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        [Authorize(Roles = "Admin,Employee")]
        // GET: Inventory/Create
        public IActionResult Create()
        {
            LoadCategories();
            return View(new Inventory());
        }

        [Authorize(Roles = "Admin,Employee")]
        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PartName,Description,CategoryId,QuantityInStock,UnitPrice,ReorderLevel,SupplierInfo")] Inventory inventory)
        {
            if (inventory.QuantityInStock < 0)
            {
                ModelState.AddModelError(nameof(inventory.QuantityInStock), "Количество не может быть отрицательным.");
            }

            if (inventory.UnitPrice < 0)
            {
                ModelState.AddModelError(nameof(inventory.UnitPrice), "Цена не может быть отрицательной.");
            }

            if (inventory.ReorderLevel < 0)
            {
                ModelState.AddModelError(nameof(inventory.ReorderLevel), "Минимальный запас не может быть отрицательным.");
            }

            ModelState.Remove(nameof(Inventory.PartCategory));
            ModelState.Remove(nameof(Inventory.OrderParts));

            if (ModelState.IsValid)
            {
                inventory.IsActive = true;
                _context.Add(inventory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Позиция успешно добавлена на склад.";
                return RedirectToAction(nameof(Index));
            }

            LoadCategories(inventory.CategoryId);
            return View(inventory);
        }

        [Authorize(Roles = "Admin,Employee")]
        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.PartId == id && i.IsActive);

            if (inventory == null)
            {
                return NotFound();
            }

            LoadCategories(inventory.CategoryId);
            return View(inventory);
        }

        [Authorize(Roles = "Admin,Employee")]
        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("PartId,PartName,Description,CategoryId,QuantityInStock,UnitPrice,ReorderLevel,SupplierInfo,IsActive")] Inventory inventory)
        {
            if (inventory.QuantityInStock < 0)
            {
                ModelState.AddModelError(nameof(inventory.QuantityInStock), "Количество не может быть отрицательным.");
            }

            if (inventory.UnitPrice < 0)
            {
                ModelState.AddModelError(nameof(inventory.UnitPrice), "Цена не может быть отрицательной.");
            }

            if (inventory.ReorderLevel < 0)
            {
                ModelState.AddModelError(nameof(inventory.ReorderLevel), "Минимальный запас не может быть отрицательным.");
            }

            ModelState.Remove(nameof(Inventory.PartCategory));
            ModelState.Remove(nameof(Inventory.OrderParts));

            if (!ModelState.IsValid)
            {
                LoadCategories(inventory.CategoryId);
                return View(inventory);
            }

            var existingInventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.PartId == inventory.PartId && i.IsActive);

            if (existingInventory == null)
            {
                return NotFound();
            }

            existingInventory.PartName = inventory.PartName;
            existingInventory.Description = inventory.Description;
            existingInventory.CategoryId = inventory.CategoryId;
            existingInventory.QuantityInStock = inventory.QuantityInStock;
            existingInventory.UnitPrice = inventory.UnitPrice;
            existingInventory.ReorderLevel = inventory.ReorderLevel;
            existingInventory.SupplierInfo = inventory.SupplierInfo;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Позиция склада успешно обновлена.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Employee")]
        // GET: Inventory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventory
                .Include(i => i.PartCategory)
                .FirstOrDefaultAsync(m => m.PartId == id && m.IsActive);

            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // POST: Inventory/Delete/5
        [Authorize(Roles = "Admin,Employee")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory != null)
            {
                inventory.IsActive = false;
                _context.Update(inventory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Позиция удалена из активного склада.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Employee")]
        // GET: Inventory/LowStock
        public async Task<IActionResult> LowStock()
        {
            var lowStockItems = await _context.Inventory
                .Include(i => i.PartCategory)
                .Where(i => i.QuantityInStock <= i.ReorderLevel && i.IsActive)
                .OrderBy(i => i.QuantityInStock)
                .ToListAsync();

            return View(lowStockItems);
        }

        [Authorize(Roles = "Admin,Employee")]
        // GET: Inventory/Restock/5
        public async Task<IActionResult> Restock(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context.Inventory
                .Include(i => i.PartCategory)
                .FirstOrDefaultAsync(i => i.PartId == id && i.IsActive);

            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        [Authorize(Roles = "Admin,Employee")]
        // POST: Inventory/Restock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(int id, int quantity)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory == null || !inventory.IsActive)
            {
                return NotFound();
            }

            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Количество пополнения должно быть больше нуля.";
                return RedirectToAction(nameof(Restock), new { id });
            }

            inventory.QuantityInStock += quantity;
            _context.Update(inventory);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Остаток успешно пополнен.";
            return RedirectToAction(nameof(Index));
        }

        private void LoadCategories(int? selectedCategoryId = null)
        {
            ViewData["CategoryId"] = new SelectList(_context.PartCategories.OrderBy(c => c.CategoryName), "CategoryId", "CategoryName", selectedCategoryId);
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventory.Any(e => e.PartId == id);
        }
    }
}
