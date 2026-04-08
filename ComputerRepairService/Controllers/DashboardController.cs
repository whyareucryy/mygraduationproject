using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.Controllers
{
    public class DashboardController : Controller
    {
        private readonly RepairDbContext _context;

        public DashboardController(RepairDbContext context)
        {
            _context = context;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var dashboardData = new
            {
                TotalOrders = await _context.ServiceOrders.CountAsync(),
                ActiveOrders = await _context.ServiceOrders.CountAsync(so => so.StatusId != 5 && so.StatusId != 6 && so.StatusId != 7),
                TotalCustomers = await _context.Customers.CountAsync(c => c.IsActive),
                TotalTechnicians = await _context.Technicians.CountAsync(t => t.IsActive),
                LowStockItems = await _context.Inventory.CountAsync(i => i.QuantityInStock <= i.ReorderLevel && i.IsActive),
                RecentOrders = await _context.ServiceOrders
                    .Include(so => so.Customer)
                    .Include(so => so.OrderStatus)
                    .OrderByDescending(so => so.CreatedDate)
                    .Take(5)
                    .ToListAsync(),
                OrdersByStatus = await _context.ServiceOrders
                    .Include(so => so.OrderStatus)
                    .GroupBy(so => so.OrderStatus.StatusName)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync()
            };

            ViewBag.DashboardData = dashboardData;
            return View();
        }
    }
}