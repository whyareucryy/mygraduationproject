using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    [Authorize(Roles = "Admin")] // Только админ
    public class CustomersController : Controller
    {
        private readonly RepairDbContext _context;

        public CustomersController(RepairDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            return View(await _context.Customers.Where(c => c.IsActive).ToListAsync());
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.ServiceOrders)
                    .ThenInclude(so => so.OrderStatus)
                .FirstOrDefaultAsync(m => m.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,Phone,Address")] Customer customer)
        {
            Console.WriteLine($"=== CREATE CUSTOMER STARTED ===");

            // ОТКЛЮЧАЕМ ВАЛИДАЦИЮ НАВИГАЦИОННЫХ СВОЙСТВ
            ModelState.Remove("ServiceOrders");

            if (ModelState.IsValid)
            {
                try
                {
                    customer.RegistrationDate = DateTime.Now;
                    customer.IsActive = true;
                    _context.Add(customer);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Customer created with ID: {customer.CustomerId}");
                    TempData["SuccessMessage"] = "Клиент успешно добавлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR creating customer: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка при создании клиента: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }

            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,FirstName,LastName,Email,Phone,Address,RegistrationDate,IsActive")] Customer customer)
        {
            Console.WriteLine($"=== EDIT CUSTOMER STARTED ===");
            Console.WriteLine($"Customer ID: {id}, Model ID: {customer.CustomerId}");

            // ОТКЛЮЧАЕМ ВАЛИДАЦИЮ НАВИГАЦИОННЫХ СВОЙСТВ
            ModelState.Remove("ServiceOrders");

            if (id != customer.CustomerId)
            {
                Console.WriteLine("ID mismatch");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Находим существующего клиента
                    var existingCustomer = await _context.Customers.FindAsync(id);
                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    // Обновляем только разрешенные поля
                    existingCustomer.FirstName = customer.FirstName;
                    existingCustomer.LastName = customer.LastName;
                    existingCustomer.Email = customer.Email;
                    existingCustomer.Phone = customer.Phone;
                    existingCustomer.Address = customer.Address;
                    existingCustomer.IsActive = customer.IsActive;
                    // RegistrationDate НЕ обновляем - она должна остаться оригинальной

                    _context.Update(existingCustomer);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Customer updated successfully");
                    TempData["SuccessMessage"] = "Данные клиента успешно обновлены!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"Concurrency error: {ex.Message}");
                    if (!CustomerExists(customer.CustomerId))
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
                    Console.WriteLine($"ERROR updating customer: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка при обновлении клиента: {ex.Message}");
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

            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Console.WriteLine($"=== DELETE CUSTOMER STARTED ===");

            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer != null)
                {
                    customer.IsActive = false;
                    _context.Update(customer);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Customer soft-deleted successfully");
                    TempData["SuccessMessage"] = "Клиент успешно удален!";
                }
                else
                {
                    Console.WriteLine("Customer not found for deletion");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR deleting customer: {ex.Message}");
                TempData["ErrorMessage"] = $"Ошибка при удалении клиента: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Search
        public async Task<IActionResult> Search(string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                return View("Index", await _context.Customers.Where(c => c.IsActive).ToListAsync());
            }

            var customers = await _context.Customers
                .Where(c => c.IsActive &&
                           (c.FirstName.Contains(searchString) ||
                            c.LastName.Contains(searchString) ||
                            c.Email.Contains(searchString) ||
                            c.Phone.Contains(searchString)))
                .ToListAsync();

            ViewBag.SearchString = searchString;
            return View("Index", customers);
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.CustomerId == id);
        }
    }
}