namespace Qutora.Application.Interfaces;

/// <summary>
/// Service for handling approval-related email notifications
/// </summary>
public interface IApprovalEmailService
{
    /// <summary>
    /// Sends approval request emails to all assigned approvers
    /// </summary>
    Task SendApprovalRequestEmailsAsync(Guid approvalRequestId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends approval decision email to the original requester
    /// </summary>
    Task SendApprovalDecisionEmailAsync(Guid approvalRequestId, CancellationToken cancellationToken = default);
} 