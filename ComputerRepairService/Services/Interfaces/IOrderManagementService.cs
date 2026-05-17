using ComputerRepairService.Models.Entities;
using ComputerRepairService.Models.ViewModels;

namespace ComputerRepairService.Services.Interfaces
{
    public interface IOrderManagementService
    {
        Task<ServiceOrder> CreateOrderAsync(OrderCreateViewModel model, string? currentUserId, IList<string> userRoles);
        Task<ServiceOrder> EditOrderAsync(OrderEditViewModel model, string? currentUserName);
        Task<bool> TakeOrderInWorkAsync(int orderId, string? userId, string? userName, bool isEmployee);
        Task<bool> CancelOrderAsync(int orderId, int customerId, string? userName);
    }
}
