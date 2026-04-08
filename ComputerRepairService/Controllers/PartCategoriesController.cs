using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    public class PartCategoriesController : Controller
    {
        private readonly RepairDbContext _context;

        public PartCategoriesController(RepairDbContext context)
        {
            _context = context;
        }

        // GET: PartCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.PartCategories.ToListAsync());
        }

        // GET: PartCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partCategory = await _context.PartCategories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (partCategory == null)
            {
                return NotFound();
            }

            return View(partCategory);
        }

        // GET: PartCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PartCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PartCategory partCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(partCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(partCategory);
        }

        // GET: PartCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partCategory = await _context.PartCategories.FindAsync(id);
            if (partCategory == null)
            {
                return NotFound();
            }
            return View(partCategory);
        }

        // POST: PartCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PartCategory partCategory)
        {
            if (id != partCategory.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(partCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PartCategoryExists(partCategory.CategoryId))
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
            return View(partCategory);
        }

        // GET: PartCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var partCategory = await _context.PartCategories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (partCategory == null)
            {
                return NotFound();
            }

            return View(partCategory);
        }

        // POST: PartCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var partCategory = await _context.PartCategories.FindAsync(id);
            if (partCategory != null)
            {
                _context.PartCategories.Remove(partCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PartCategoryExists(int id)
        {
            return _context.PartCategories.Any(e => e.CategoryId == id);
        }
    }
}