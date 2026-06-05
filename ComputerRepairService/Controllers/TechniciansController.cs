using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.ViewModels;
using ComputerRepairService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class TechniciansController : Controller
    {
        private readonly RepairDbContext _context;
        private readonly IAdminAccountProvisioningService _accountService;

        public TechniciansController(RepairDbContext context, IAdminAccountProvisioningService accountService)
        {
            _context = context;
            _accountService = accountService;
        }

        // GET: Technicians
        public async Task<IActionResult> Index()
        {
            return View(await _context.Technicians.Where(t => t.IsActive).ToListAsync());
        }

        // GET: Technicians/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var technician = await _context.Technicians
                .Include(t => t.OrderTechnicians)
                    .ThenInclude(ot => ot.ServiceOrder)
                        .ThenInclude(so => so.Customer)
                .Include(t => t.OrderTechnicians)
                    .ThenInclude(ot => ot.ServiceOrder)
                        .ThenInclude(so => so.OrderStatus)
                .Include(t => t.OrderServices)
                    .ThenInclude(os => os.ServiceOrder)
                .FirstOrDefaultAsync(m => m.TechnicianId == id);

            if (technician == null)
            {
                return NotFound();
            }

            return View(technician);
        }


        // GET: Technicians/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new TechnicianCreateViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TechnicianCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var technician = new Technician
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone?.Trim() ?? string.Empty,
                Specialization = model.Specialization?.Trim() ?? string.Empty,
                HourlyRate = model.HourlyRate,
                IsActive = model.IsActive
            };

            var (success, error) = await _accountService.CreateTechnicianWithAccountAsync(technician, model.Password);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Не удалось создать мастера.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Мастер {technician.FirstName} {technician.LastName} создан. Вход: {technician.Email}";
            return RedirectToAction(nameof(Index));
        }


        // GET: Technicians/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var technician = await _context.Technicians.FindAsync(id);
            if (technician == null)
            {
                return NotFound();
            }
            return View(technician);
        }

        // POST: Technicians/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TechnicianId,FirstName,LastName,Email,Phone,Specialization,HireDate,HourlyRate,IsActive")] Technician technician)
        {
            Console.WriteLine($"=== EDIT TECHNICIAN STARTED ===");
            Console.WriteLine($"Technician ID: {id}, Model ID: {technician.TechnicianId}");

            // Отключаем валидацию навигационных свойств
            ModelState.Remove("OrderTechnicians");
            ModelState.Remove("OrderServices");

            if (id != technician.TechnicianId)
            {
                Console.WriteLine("ID mismatch");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Находим существующего мастера
                    var existingTechnician = await _context.Technicians.FindAsync(id);
                    if (existingTechnician == null)
                    {
                        return NotFound();
                    }

                    // Обновляем только разрешенные поля
                    existingTechnician.FirstName = technician.FirstName;
                    existingTechnician.LastName = technician.LastName;
                    existingTechnician.Email = technician.Email;
                    existingTechnician.Phone = technician.Phone;
                    existingTechnician.Specialization = technician.Specialization;
                    existingTechnician.HourlyRate = technician.HourlyRate;
                    existingTechnician.IsActive = technician.IsActive;
                    // HireDate НЕ обновляем - она должна остаться оригинальной

                    _context.Update(existingTechnician);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Technician updated successfully");
                    TempData["SuccessMessage"] = "Данные мастера успешно обновлены!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"Concurrency error: {ex.Message}");
                    if (!TechnicianExists(technician.TechnicianId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR updating technician: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка при обновлении мастера: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("ModelState is invalid in Edit");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }

            return View(technician);
        }

        // GET: Technicians/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var technician = await _context.Technicians
                .FirstOrDefaultAsync(m => m.TechnicianId == id);
            if (technician == null)
            {
                return NotFound();
            }

            return View(technician);
        }

        // POST: Technicians/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Console.WriteLine($"=== DELETE TECHNICIAN STARTED ===");

            try
            {
                var technician = await _context.Technicians.FindAsync(id);
                if (technician != null)
                {
                    technician.IsActive = false;
                    _context.Update(technician);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Technician soft-deleted successfully");
                    TempData["SuccessMessage"] = "Мастер успешно удален!";
                }
                else
                {
                    Console.WriteLine("Technician not found for deletion");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR deleting technician: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при удалении мастера: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Technicians/Workload
        public async Task<IActionResult> Workload()
        {
            try
            {
                var techniciansWorkload = await _context.Technicians
                    .Where(t => t.IsActive)
                    .Select(t => new
                    {
                        Technician = t,
                        ActiveOrders = t.OrderTechnicians.Count(ot =>
                            ot.ServiceOrder.StatusId != 5 && // Не готовые
                            ot.ServiceOrder.StatusId != 6 && // Не выданные  
                            ot.ServiceOrder.StatusId != 7),  // Не отмененные
                        TotalHours = t.OrderTechnicians.Sum(ot => ot.HoursWorked)
                    })
                    .OrderByDescending(t => t.ActiveOrders)
                    .ThenByDescending(t => t.TotalHours)
                    .ToListAsync();

                ViewBag.TechniciansWorkload = techniciansWorkload;

                Console.WriteLine($"Workload data loaded for {techniciansWorkload.Count} technicians");
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR loading workload data: {ex.Message}");
                TempData["ErrorMessage"] = "Ошибка при загрузке данных о нагрузке";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool TechnicianExists(int id)
        {
            return _context.Technicians.Any(e => e.TechnicianId == id);
        }
    }
}