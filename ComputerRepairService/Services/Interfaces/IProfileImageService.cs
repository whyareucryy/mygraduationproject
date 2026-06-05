namespace ComputerRepairService.Services.Interfaces
{
    public interface IProfileImageService
    {
        Task<(bool Success, string? Error, string? RelativePath)> SaveAvatarAsync(
            IFormFile file, string userId, string? currentPath);

        Task DeleteAvatarFileAsync(string? relativePath);
    }
}
