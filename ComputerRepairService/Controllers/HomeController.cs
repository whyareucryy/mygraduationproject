using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly RepairDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            RepairDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                ViewBag.UserName = $"{user.FirstName} {user.LastName}";

                if (User.IsInRole("Admin"))
                {
                    ViewBag.Role = "Администратор";
                    ViewBag.Stats = new
                    {
                        TotalOrders = await _context.ServiceOrders.CountAsync(),
                        ActiveOrders = await _context.ServiceOrders.CountAsync(o => o.StatusId != 5 && o.StatusId != 6 && o.StatusId != 7),
                        TotalCustomers = await _context.Customers.CountAsync(),
                        LowStockItems = await _context.Inventory.CountAsync(i => i.QuantityInStock <= i.ReorderLevel)
                    };
                }
                else if (User.IsInRole("Employee"))
                {
                    ViewBag.Role = "Сотрудник";
                    var technician = await _context.Technicians
                        .FirstOrDefaultAsync(t => t.UserId == user.Id);

                    if (technician != null)
                    {
                        ViewBag.AssignedOrders = await _context.ServiceOrders
                            .CountAsync(o => o.OrderTechnicians.Any(ot => ot.TechnicianId == technician.TechnicianId));
                    }
                }
                else if (User.IsInRole("Client"))
                {
                    ViewBag.Role = "Клиент";
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == user.Id);

                    if (customer != null)
                    {
                        ViewBag.MyOrders = await _context.ServiceOrders
                            .CountAsync(o => o.CustomerId == customer.CustomerId);
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [Route("access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}