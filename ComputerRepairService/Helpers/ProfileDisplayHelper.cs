using ComputerRepairService.Models.Entities;

namespace ComputerRepairService.Helpers
{
    public static class ProfileDisplayHelper
    {
        public static string? GetAvatarUrl(ApplicationUser? user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.ProfileImagePath))
            {
                return null;
            }

            var path = user.ProfileImagePath.StartsWith('/')
                ? user.ProfileImagePath
                : "/" + user.ProfileImagePath.TrimStart('/');

            var cacheToken = user.SecurityStamp ?? user.Id;
            return $"{path}?v={Uri.EscapeDataString(cacheToken)}";
        }

        public static string GetDisplayName(ApplicationUser? user)
        {
            if (user == null)
            {
                return "Пользователь";
            }

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            return user.Email ?? user.UserName ?? "Пользователь";
        }

        public static string GetInitials(ApplicationUser? user)
        {
            if (user == null)
            {
                return "?";
            }

            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                var first = char.ToUpper(user.FirstName.Trim()[0]);
                if (!string.IsNullOrWhiteSpace(user.LastName))
                {
                    return $"{first}{char.ToUpper(user.LastName.Trim()[0])}";
                }

                return first.ToString();
            }

            var source = user.Email ?? user.UserName;
            return string.IsNullOrWhiteSpace(source) ? "?" : char.ToUpper(source[0]).ToString();
        }
    }
}
