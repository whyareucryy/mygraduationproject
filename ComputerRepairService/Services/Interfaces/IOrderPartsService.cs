using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.Services.Interfaces
{
    public interface IOrderPartsService
    {
        Task<ServiceOrder?> GetOrderForPartsManagementAsync(int orderId, string? userId, bool isAdmin, bool isEmployee);

        bool CanManageParts(ServiceOrder order);

        decimal GetPartsTotal(IEnumerable<OrderPart> parts);

        Task<(bool Success, string? Error, string? Warning)> AddPartToOrderAsync(
            int orderId, int partId, int quantity, string changedBy, string? userId, bool isAdmin, bool isEmployee);

        Task<(bool Success, string? Error)> RemovePartFromOrderAsync(
            int orderPartId, string changedBy, string? userId, bool isAdmin, bool isEmployee);

        Task RestoreStockForOrderAsync(int orderId);
    }
}
