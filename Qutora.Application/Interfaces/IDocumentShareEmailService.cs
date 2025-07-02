namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for handling document share email notifications
/// </summary>
public interface IDocumentShareEmailService
{
    /// <summary>
    /// Sends document share notification emails to specified recipients
    /// </summary>
    /// <param name="shareId">The document share ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendDocumentShareNotificationsAsync(Guid shareId, CancellationToken cancellationToken = default);
} 