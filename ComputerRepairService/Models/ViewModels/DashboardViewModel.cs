using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.Models.ViewModels
{
    public class DashboardViewModel
    {
        public bool IsAdmin { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalTechnicians { get; set; }
        public int LowStockItems { get; set; }
        public int IncomingClientRequests { get; set; }
        public int OrdersCreatedToday { get; set; }
        public int OverdueOrders { get; set; }
        public int UrgentOrders { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal AverageCheckThisMonth { get; set; }
        public double AverageCompletionDays { get; set; }
        public List<ServiceOrder> RecentOrders { get; set; } = new();
        public List<StatusCountItem> OrdersByStatus { get; set; } = new();
        public List<TechnicianWorkloadItem> TopTechnicians { get; set; } = new();
    }

    public class StatusCountItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TechnicianWorkloadItem
    {
        public string TechnicianName { get; set; } = string.Empty;
        public int ActiveOrders { get; set; }
    }
}
