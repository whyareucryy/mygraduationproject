using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ComputerRepairService.Data;
using ComputerRepairService.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using ComputerRepairService.Models.ViewModels;

namespace ComputerRepairService.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
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
            var now = DateTime.Now;
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var statuses = await _context.OrderStatuses
                .ToDictionaryAsync(s => s.StatusName.Trim().ToLower(), s => s.StatusId);

            var newStatusId = statuses.TryGetValue("новая", out var newId) ? newId : 1;
            var diagnosticStatusId = statuses.TryGetValue("диагностика", out var diagId) ? diagId : 2;
            var repairStatusId = statuses.TryGetValue("в ремонте", out var repairId) ? repairId : 4;
            var readyStatusId = statuses.TryGetValue("готово", out var readyId) ? readyId : 5;
            var issuedStatusId = statuses.TryGetValue("выдано", out var issuedId) ? issuedId : 6;
            var cancelledStatusId = statuses.TryGetValue("отменено", out var cancelledId) ? cancelledId : 7;

            var activeStatusIds = new[] { newStatusId, diagnosticStatusId, repairStatusId };
            var closedStatusIds = new[] { readyStatusId, issuedStatusId, cancelledStatusId };

            var totalOrders = await _context.ServiceOrders.CountAsync();
            var activeOrders = await _context.ServiceOrders.CountAsync(so => activeStatusIds.Contains(so.StatusId));
            var totalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
            var totalTechnicians = await _context.Technicians.CountAsync(t => t.IsActive);
            var lowStockItems = await _context.Inventory.CountAsync(i => i.QuantityInStock <= i.ReorderLevel && i.IsActive);
            var incomingClientRequests = await _context.ServiceOrders.CountAsync(so => so.StatusId == newStatusId && !so.OrderTechnicians.Any());
            var ordersCreatedToday = await _context.ServiceOrders.CountAsync(so => so.CreatedDate >= today && so.CreatedDate < tomorrow);
            var overdueOrders = await _context.ServiceOrders.CountAsync(so =>
                so.EstimatedCompletionDate.HasValue &&
                so.EstimatedCompletionDate.Value < now &&
                !so.ActualCompletionDate.HasValue &&
                !closedStatusIds.Contains(so.StatusId));
            var urgentOrders = await _context.ServiceOrders.CountAsync(so => so.Priority >= 4 && activeStatusIds.Contains(so.StatusId));

            var monthlyCompletedOrders = _context.ServiceOrders
                .Where(so => so.ActualCompletionDate.HasValue && so.ActualCompletionDate.Value >= monthStart);

            var revenueThisMonth = await monthlyCompletedOrders.SumAsync(so => (decimal?)so.TotalCost) ?? 0m;
            var averageCheckThisMonth = await monthlyCompletedOrders
                .Where(so => so.TotalCost > 0)
                .AverageAsync(so => (decimal?)so.TotalCost) ?? 0m;

            var averageCompletionDays = await _context.ServiceOrders
                .Where(so => so.ActualCompletionDate.HasValue && so.CreatedDate >= monthStart)
                .AverageAsync(so => (double?)EF.Functions.DateDiffDay(so.CreatedDate, so.ActualCompletionDate!.Value)) ?? 0;

            var recentOrders = await _context.ServiceOrders
                .Include(so => so.Customer)
                .Include(so => so.OrderStatus)
                .OrderByDescending(so => so.CreatedDate)
                .Take(8)
                .ToListAsync();

            var ordersByStatus = await _context.ServiceOrders
                .Include(so => so.OrderStatus)
                .GroupBy(so => so.OrderStatus.StatusName)
                .Select(g => new StatusCountItem
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var topTechnicians = await _context.OrderTechnicians
                .Where(ot =>
                    ot.Technician.IsActive &&
                    !closedStatusIds.Contains(ot.ServiceOrder.StatusId))
                .GroupBy(ot => new { ot.TechnicianId, ot.Technician.FirstName, ot.Technician.LastName })
                .Select(g => new TechnicianWorkloadItem
                {
                    TechnicianName = $"{g.Key.FirstName} {g.Key.LastName}",
                    ActiveOrders = g.Select(x => x.OrderId).Distinct().Count()
                })
                .OrderByDescending(x => x.ActiveOrders)
                .Take(5)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                IsAdmin = User.IsInRole("Admin"),
                TotalOrders = totalOrders,
                ActiveOrders = activeOrders,
                TotalCustomers = totalCustomers,
                TotalTechnicians = totalTechnicians,
                LowStockItems = lowStockItems,
                IncomingClientRequests = incomingClientRequests,
                OrdersCreatedToday = ordersCreatedToday,
                OverdueOrders = overdueOrders,
                UrgentOrders = urgentOrders,
                RevenueThisMonth = revenueThisMonth,
                AverageCheckThisMonth = averageCheckThisMonth,
                AverageCompletionDays = averageCompletionDays,
                RecentOrders = recentOrders,
                OrdersByStatus = ordersByStatus,
                TopTechnicians = topTechnicians
            };

            return View(model);
        }
    }
}