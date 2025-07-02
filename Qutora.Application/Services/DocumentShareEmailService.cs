using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities.Identity;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.UnitOfWork;

namespace Qutora.Application.Services;

/// <summary>
/// Service for handling document share email notifications
/// Uses its own scope to avoid DbContext concurrency issues
/// </summary>
public class DocumentShareEmailService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    ILogger<DocumentShareEmailService> logger)
    : IDocumentShareEmailService
{
    public async Task SendDocumentShareNotificationsAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        try
        {
            var share = await unitOfWork.DocumentShares.GetByIdAsync(shareId, cancellationToken);
            if (share == null)
            {
                logger.LogWarning("Document share not found: {ShareId}", shareId);
                return;
            }

            if (string.IsNullOrWhiteSpace(share.NotificationEmails))
            {
                logger.LogInformation("No notification emails configured for share: {ShareId}", shareId);
                return;
            }

            var document = await unitOfWork.Documents.GetByIdAsync(share.DocumentId, cancellationToken);
            if (document == null)
            {
                logger.LogWarning("Document not found for share: {ShareId}", shareId);
                return;
            }

            var sharer = await userManager.FindByIdAsync(share.CreatedBy);
            var sharerName = sharer != null ? $"{sharer.FirstName} {sharer.LastName}".Trim() : "Unknown User";

            var shareUrl = $"/document/{share.ShareCode}";

            // Get notification emails
            var notificationEmails = new List<string>();
            try
            {
                notificationEmails = JsonSerializer.Deserialize<List<string>>(share.NotificationEmails) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Error deserializing notification emails for share {ShareId}", shareId);
                return;
            }

            // Send email to each recipient
            foreach (var email in notificationEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                await emailService.SendDocumentShareNotificationAsync(
                    email,
                    "Dear User", // We don't have recipient names, so use generic greeting
                    document.Name,
                    sharerName,
                    share.ShareCode,
                    shareUrl,
                    share.ExpiresAt ?? DateTime.UtcNow.AddDays(30), // Default expiry if not set
                    share.CustomMessage ?? ""
                );
            }

            logger.LogInformation("Successfully sent document share notifications for {ShareId} to {EmailCount} recipients", 
                shareId, notificationEmails.Count(e => !string.IsNullOrWhiteSpace(e)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send document share notifications for {ShareId}", shareId);
        }
    }
} 