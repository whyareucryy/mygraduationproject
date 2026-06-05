using ComputerRepairService.Services.Interfaces;

namespace ComputerRepairService.Services
{
    public class ProfileImageService : IProfileImageService
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const string AvatarFolder = "uploads/avatars";

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileImageService> _logger;

        public ProfileImageService(IWebHostEnvironment environment, ILogger<ProfileImageService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, string? RelativePath)> SaveAvatarAsync(
            IFormFile file, string userId, string? currentPath)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "Выберите файл изображения.", null);
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return (false, "Размер файла не должен превышать 5 МБ.", null);
            }

            var extension = ResolveExtension(file);
            if (extension == null || !AllowedExtensions.Contains(extension))
            {
                return (false, "Допустимые форматы: JPG, PNG, WEBP.", null);
            }

            var uploadsDir = Path.Combine(_environment.WebRootPath, AvatarFolder);
            Directory.CreateDirectory(uploadsDir);

            var safeUserId = SanitizeUserId(userId);
            var fileName = $"{safeUserId}{extension}";
            var physicalPath = Path.Combine(uploadsDir, fileName);
            var relativePath = $"/{AvatarFolder}/{fileName}".Replace('\\', '/');

            try
            {
                if (!string.IsNullOrWhiteSpace(currentPath) && currentPath != relativePath)
                {
                    await DeleteAvatarFileAsync(currentPath);
                }

                await using var stream = new FileStream(physicalPath, FileMode.Create);
                await file.CopyToAsync(stream);

                return (true, null, relativePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось сохранить аватар для пользователя {UserId}", userId);
                return (false, "Не удалось сохранить изображение.", null);
            }
        }

        public Task DeleteAvatarFileAsync(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return Task.CompletedTask;
            }

            try
            {
                var normalized = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var physicalPath = Path.Combine(_environment.WebRootPath, normalized);

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить файл аватара {Path}", relativePath);
            }

            return Task.CompletedTask;
        }

        private static string SanitizeUserId(string userId)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(userId.Where(ch => !invalid.Contains(ch)).ToArray());
        }

        private static string? ResolveExtension(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(extension))
            {
                return extension;
            }

            return file.ContentType.ToLowerInvariant() switch
            {
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => null
            };
        }
    }
}
