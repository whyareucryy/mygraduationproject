using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ComputerRepairService.Services.Identity;

public class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;
    private readonly IWebHostEnvironment _environment;

    public DevelopmentEmailSender(
        ILogger<DevelopmentEmailSender> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var appDataPath = Path.Combine(_environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(appDataPath);

        var logFilePath = Path.Combine(appDataPath, "identity-emails.log");
        var createdAt = DateTimeOffset.UtcNow;

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("========================");
        messageBuilder.AppendLine($"UTC: {createdAt:O}");
        messageBuilder.AppendLine($"To: {email}");
        messageBuilder.AppendLine($"Subject: {subject}");
        messageBuilder.AppendLine("Body:");
        messageBuilder.AppendLine(htmlMessage);
        messageBuilder.AppendLine();

        var message = messageBuilder.ToString();

        await File.AppendAllTextAsync(logFilePath, message);

        _logger.LogInformation(
            "Identity email intercepted in development mode. To: {Email}. Subject: {Subject}. Saved to: {LogFilePath}",
            email,
            subject,
            logFilePath);
    }
}
