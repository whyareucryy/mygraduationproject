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
            return View(await _context.DeviceTypes
                .OrderBy(dt => dt.TypeName)
                .ToListAsync());
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

            ViewBag.RelatedOrdersCount = await _context.ServiceOrders
                .CountAsync(so => so.DeviceTypeId == deviceType.DeviceTypeId);

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
        public async Task<IActionResult> Create([Bind("TypeName,Description")] DeviceType deviceType)
        {
            deviceType.TypeName = deviceType.TypeName?.Trim() ?? string.Empty;

            if (await _context.DeviceTypes.AnyAsync(dt => dt.TypeName == deviceType.TypeName))
            {
                ModelState.AddModelError(nameof(DeviceType.TypeName), "Тип устройства с таким названием уже существует.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(deviceType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Тип устройства добавлен.";
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
        public async Task<IActionResult> Edit(int id, [Bind("DeviceTypeId,TypeName,Description")] DeviceType deviceType)
        {
            if (id != deviceType.DeviceTypeId)
            {
                return NotFound();
            }

            deviceType.TypeName = deviceType.TypeName?.Trim() ?? string.Empty;

            if (await _context.DeviceTypes.AnyAsync(dt => dt.TypeName == deviceType.TypeName && dt.DeviceTypeId != deviceType.DeviceTypeId))
            {
                ModelState.AddModelError(nameof(DeviceType.TypeName), "Тип устройства с таким названием уже существует.");
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

                    throw;
                }

                TempData["SuccessMessage"] = "Тип устройства обновлён.";
                return RedirectToAction(nameof(Index));
            }

            return View(deviceType);
        }

        // GET: DeviceTypes/Delete/5
        [Authorize(Roles = "Admin")]
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

            ViewBag.RelatedOrdersCount = await _context.ServiceOrders
                .CountAsync(so => so.DeviceTypeId == deviceType.DeviceTypeId);

            return View(deviceType);
        }

        // POST: DeviceTypes/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deviceType = await _context.DeviceTypes.FindAsync(id);
            if (deviceType == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (await _context.ServiceOrders.AnyAsync(so => so.DeviceTypeId == id))
            {
                TempData["ErrorMessage"] = "Нельзя удалить тип устройства: есть заказы, в которых он указан. Сначала измените или удалите эти заказы.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.DeviceTypes.Remove(deviceType);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Тип устройства удалён.";
            }
            catch (DbUpdateException)
            {
                _context.Entry(deviceType).State = EntityState.Unchanged;
                TempData["ErrorMessage"] = "Не удалось удалить тип устройства из-за связанных данных в базе.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DeviceTypeExists(int id)
        {
            return _context.DeviceTypes.Any(e => e.DeviceTypeId == id);
        }
    }
}
