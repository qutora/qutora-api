using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.Enums;
using Qutora.Shared.Helpers;

namespace Qutora.Application.Services;

/// <summary>
/// Service for handling approval-related email notifications
/// Uses its own scope to avoid DbContext concurrency issues
/// </summary>
public class ApprovalEmailService(
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<ApprovalEmailService> logger) : IApprovalEmailService
{
    public async Task SendApprovalRequestEmailsAsync(Guid approvalRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await unitOfWork.ShareApprovalRequests.GetByIdAsync(approvalRequestId, cancellationToken);
            if (request == null)
            {
                logger.LogWarning("Approval request not found: {RequestId}", approvalRequestId);
                return;
            }

            var documentShare = await unitOfWork.DocumentShares.GetByIdAsync(request.DocumentShareId, cancellationToken);
            if (documentShare == null)
            {
                logger.LogWarning("Document share not found for approval request: {RequestId}", approvalRequestId);
                return;
            }

            var document = await unitOfWork.Documents.GetByIdAsync(documentShare.DocumentId, cancellationToken);
            if (document == null)
            {
                logger.LogWarning("Document not found for approval request: {RequestId}", approvalRequestId);
                return;
            }

            // Get requester info
            var requester = await userManager.FindByIdAsync(request.RequestedByUserId);
            var requesterName = requester != null ? $"{requester.FirstName} {requester.LastName}".Trim() : "Unknown User";

            // Get category name
            string categoryName = "Uncategorized";
            if (document.CategoryId.HasValue)
            {
                var category = await unitOfWork.Categories.GetByIdAsync(document.CategoryId.Value, cancellationToken);
                categoryName = category?.Name ?? "Uncategorized";
            }

            // Get policy name
            string policyName = "System Policy";
            var policy = await unitOfWork.ApprovalPolicies.GetByIdAsync(request.ApprovalPolicyId, cancellationToken);
            policyName = policy?.Name ?? "System Policy";

            // Get approvers
            var approvers = await GetUsersWithApprovalPermissionAsync();
            
            // Send emails
            foreach (var approver in approvers.Where(u => !string.IsNullOrEmpty(u.Email)))
            {
                await emailService.SendApprovalRequestNotificationAsync(
                    approver.Email,
                    $"{approver.FirstName} {approver.LastName}".Trim(),
                    document.Name,
                    requesterName,
                    request.RequestReason ?? "No reason provided",
                    documentShare.ShareCode,
                    request.ExpiresAt,
                    categoryName,
                    FileHelper.FormatFileSize(document.FileSizeBytes),
                    policyName
                );
            }

            logger.LogInformation("Successfully sent approval request emails for {RequestId} to {EmailCount} approvers", 
                approvalRequestId, approvers.Count(u => !string.IsNullOrEmpty(u.Email)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send approval request emails for {RequestId}", approvalRequestId);
        }
    }

    public async Task SendApprovalDecisionEmailAsync(Guid approvalRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await unitOfWork.ShareApprovalRequests.GetByIdAsync(approvalRequestId, cancellationToken);
            if (request == null)
            {
                logger.LogWarning("Approval request not found: {RequestId}", approvalRequestId);
                return;
            }

            var documentShare = await unitOfWork.DocumentShares.GetByIdAsync(request.DocumentShareId, cancellationToken);
            if (documentShare == null) return;

            var document = await unitOfWork.Documents.GetByIdAsync(documentShare.DocumentId, cancellationToken);
            if (document == null) return;

            var requester = await userManager.FindByIdAsync(request.RequestedByUserId);
            if (requester == null || string.IsNullOrEmpty(requester.Email)) return;

            var requesterName = $"{requester.FirstName} {requester.LastName}".Trim();
            var decision = request.Status == ApprovalStatus.Approved ? "Approved" : "Rejected";
            var shareUrl = $"/document/{documentShare.ShareCode}";

            await emailService.SendApprovalDecisionNotificationAsync(
                requester.Email,
                requesterName,
                document.Name,
                decision,
                request.FinalComment ?? "No additional comments",
                documentShare.ShareCode,
                shareUrl
            );

            logger.LogInformation("Sent approval decision notification for {RequestId} to user {UserId}", 
                approvalRequestId, requester.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send approval decision email for {RequestId}", approvalRequestId);
        }
    }

    private async Task<List<ApplicationUser>> GetUsersWithApprovalPermissionAsync()
    {
        // First, find all roles that have Approval.Process permission
        var rolesWithApprovalPermission = new List<string>();
        var allRoles = roleManager.Roles.ToList();
        
        foreach (var role in allRoles)
        {
            var roleClaims = await roleManager.GetClaimsAsync(role);
            if (roleClaims.Any(c => c.Type == "permissions" && c.Value == "Approval.Process"))
            {
                rolesWithApprovalPermission.Add(role.Name!);
            }
        }

        if (!rolesWithApprovalPermission.Any())
        {
            return new List<ApplicationUser>();
        }

        // Get all users who have any of these roles in a single query
        var usersWithApprovalPermission = new List<ApplicationUser>();
        
        foreach (var roleName in rolesWithApprovalPermission)
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
            foreach (var user in usersInRole)
            {
                if (!usersWithApprovalPermission.Any(u => u.Id == user.Id))
                {
                    usersWithApprovalPermission.Add(user);
                }
            }
        }
        
        return usersWithApprovalPermission;
    }


} 