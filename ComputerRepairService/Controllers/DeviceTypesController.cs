using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class DeviceTypesController : Controller
    {
        private readonly RepairDbContext _context;

        public DeviceTypesController(RepairDbContext context)
        {
            _context = context;
        }

        // GET: DeviceTypes
        public async Task<IActionResult> Index()
        {
            return View(await _context.DeviceTypes.ToListAsync());
        }

        // GET: DeviceTypes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var deviceType = await _context.DeviceTypes
                .FirstOrDefaultAsync(m => m.DeviceTypeId == id);
            if (deviceType == null)
            {
                return NotFound();
            }

            return View(deviceType);
        }

        // GET: DeviceTypes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DeviceTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeviceType deviceType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(deviceType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(deviceType);
        }

        // GET: DeviceTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var deviceType = await _context.DeviceTypes.FindAsync(id);
            if (deviceType == null)
            {
                return NotFound();
            }
            return View(deviceType);
        }

        // POST: DeviceTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DeviceType deviceType)
        {
            if (id != deviceType.DeviceTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(deviceType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeviceTypeExists(deviceType.DeviceTypeId))
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
            return View(deviceType);
        }

        // GET: DeviceTypes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var deviceType = await _context.DeviceTypes
                .FirstOrDefaultAsync(m => m.DeviceTypeId == id);
            if (deviceType == null)
            {
                return NotFound();
            }

            return View(deviceType);
        }

        // POST: DeviceTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deviceType = await _context.DeviceTypes.FindAsync(id);
            if (deviceType != null)
            {
                _context.DeviceTypes.Remove(deviceType);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DeviceTypeExists(int id)
        {
            return _context.DeviceTypes.Any(e => e.DeviceTypeId == id);
        }
    }
}