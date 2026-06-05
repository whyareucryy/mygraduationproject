using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.ViewModels;
using ComputerRepairService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly RepairDbContext _context;
        private readonly IAdminAccountProvisioningService _accountService;

        public CustomersController(RepairDbContext context, IAdminAccountProvisioningService accountService)
        {
            _context = context;
            _accountService = accountService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Customers.Where(c => c.IsActive).ToListAsync());
        }

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

        public IActionResult Create()
        {
            return View(new CustomerCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = new Customer
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                Address = model.Address?.Trim() ?? string.Empty
            };

            var (success, error) = await _accountService.CreateCustomerWithAccountAsync(customer, model.Password);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Не удалось создать клиента.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Клиент {customer.FirstName} {customer.LastName} создан. Вход: {customer.Email}";
            return RedirectToAction(nameof(Index));
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,FirstName,LastName,Email,Phone,Address,RegistrationDate,IsActive")] Customer customer)
        {
            ModelState.Remove("ServiceOrders");

            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCustomer = await _context.Customers.FindAsync(id);
                    if (existingCustomer == null)
                    {
                        return NotFound();
                    }

                    existingCustomer.FirstName = customer.FirstName;
                    existingCustomer.LastName = customer.LastName;
                    existingCustomer.Email = customer.Email;
                    existingCustomer.Phone = customer.Phone;
                    existingCustomer.Address = customer.Address;
                    existingCustomer.IsActive = customer.IsActive;

                    _context.Update(existingCustomer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Данные клиента успешно обновлены!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.CustomerId))
                    {
                        return NotFound();
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Ошибка при обновлении клиента: {ex.Message}");
                }
            }

            return View(customer);
        }

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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer != null)
                {
                    customer.IsActive = false;
                    _context.Update(customer);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Клиент успешно удален!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при удалении клиента: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

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
