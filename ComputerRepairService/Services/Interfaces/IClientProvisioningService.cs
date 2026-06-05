using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.Services.Interfaces
{
    public interface IClientProvisioningService
    {
        Task<(bool Success, string? ErrorMessage)> ProvisionAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    }
}
