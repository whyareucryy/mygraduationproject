using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairService.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly RepairDbContext _context;

        public ReportsController(RepairDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/OrdersByStatus
        public async Task<IActionResult> OrdersByStatus()
        {
            var ordersByStatus = await _context.ServiceOrders
                .Include(so => so.OrderStatus)
                .GroupBy(so => so.OrderStatus.StatusName)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.OrdersByStatus = ordersByStatus;
            return View();
        }

        // GET: Reports/RevenueReport
        public async Task<IActionResult> RevenueReport(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue)
                startDate = DateTime.Now.AddMonths(-1);
            if (!endDate.HasValue)
                endDate = DateTime.Now;

            var revenueData = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(p => p.Amount) })
                .OrderBy(g => g.Date)
                .ToListAsync();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.RevenueData = revenueData;

            return View();
        }

        // GET: Reports/TechnicianPerformance
        public async Task<IActionResult> TechnicianPerformance()
        {
            var technicianPerformance = await _context.Technicians
                .Where(t => t.IsActive)
                .Select(t => new
                {
                    TechnicianName = t.FirstName + " " + t.LastName,
                    CompletedOrders = t.OrderTechnicians.Count(ot => ot.ServiceOrder.StatusId == 5), // Готовые заказы
                    TotalHours = t.OrderTechnicians.Sum(ot => ot.HoursWorked),
                    TotalRevenue = t.OrderServices.Sum(os => os.TotalPrice) +
                                  t.OrderTechnicians.Sum(ot => ot.HoursWorked * (ot.Technician.HourlyRate ?? 0))
                })
                .ToListAsync();

            ViewBag.TechnicianPerformance = technicianPerformance;
            return View();
        }

        // GET: Reports/InventoryReport
        public async Task<IActionResult> InventoryReport()
        {
            var inventoryReport = await _context.Inventory
                .Include(i => i.PartCategory)
                .Where(i => i.IsActive)
                .OrderBy(i => i.QuantityInStock)
                .ToListAsync();

            return View(inventoryReport);
        }
    }
}