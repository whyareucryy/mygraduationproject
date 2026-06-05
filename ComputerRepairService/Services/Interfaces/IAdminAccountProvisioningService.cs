using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.Services.Interfaces
{
    public interface IAdminAccountProvisioningService
    {
        Task<(bool Success, string? Error)> CreateCustomerWithAccountAsync(Customer customer, string password);

        Task<(bool Success, string? Error)> CreateTechnicianWithAccountAsync(Technician technician, string password);
    }
}
