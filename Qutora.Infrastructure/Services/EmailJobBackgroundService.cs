using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.Enums;
using Qutora.Shared.Events;
using Qutora.Shared.Helpers;

namespace Qutora.Infrastructure.Services;

/// <summary>
/// Background service that processes email events
/// </summary>
public class EmailJobBackgroundService(IServiceProvider serviceProvider, ILogger<EmailJobBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email Job Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check for events in the queue
                if (InMemoryEventPublisher.TryDequeue(out var eventItem))
                {
                    await ProcessEventAsync(eventItem.EventType, eventItem.EventData, stoppingToken);
                }
                else
                {
                    // No events, wait a bit
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing email events");
                await Task.Delay(5000, stoppingToken); // Wait longer on error
            }
        }

        logger.LogInformation("Email Job Background Service stopped");
    }

    private async Task ProcessEventAsync(Type eventType, object eventData, CancellationToken cancellationToken)
    {
        try
        {
            if (eventType == typeof(ApprovalRequestCreatedEvent))
            {
                await ProcessApprovalRequestCreatedAsync((ApprovalRequestCreatedEvent)eventData, cancellationToken);
            }
            else if (eventType == typeof(ApprovalDecisionMadeEvent))
            {
                await ProcessApprovalDecisionMadeAsync((ApprovalDecisionMadeEvent)eventData, cancellationToken);
            }
            // Other events will be added here
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process event: {EventType}", eventType.Name);
        }
    }

    private async Task ProcessApprovalRequestCreatedAsync(ApprovalRequestCreatedEvent eventData, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        try
        {
            logger.LogInformation("Processing approval request email job for {RequestId}", eventData.ApprovalRequestId);

            // Get approvers
            var approvers = await GetUsersWithApprovalPermissionAsync(userManager, roleManager);
            
            if (!approvers.Any())
            {
                logger.LogWarning("No approvers found for approval request {RequestId}", eventData.ApprovalRequestId);
                return;
            }

            // Send emails to all approvers
            var emailTasks = approvers
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .Select(async approver =>
                {
                    try
                    {
                        await emailService.SendApprovalRequestNotificationAsync(
                            approver.Email!,
                            $"{approver.FirstName} {approver.LastName}".Trim(),
                            eventData.DocumentName,
                            eventData.RequesterName,
                            eventData.RequestReason,
                            eventData.ShareCode,
                            eventData.ExpiresAt,
                            eventData.CategoryName,
                            FileHelper.FormatFileSize(eventData.FileSizeBytes),
                            eventData.PolicyName
                        );

                        logger.LogDebug("Sent approval email to user {UserId} for request {RequestId}", 
                            approver.Id, eventData.ApprovalRequestId);
                    }
                    catch (Exception ex)
                    {
                                            logger.LogError(ex, "Failed to send approval email to user {UserId} for request {RequestId}",
                        approver.Id, eventData.ApprovalRequestId);
                    }
                });

            await Task.WhenAll(emailTasks);

            logger.LogInformation("Successfully processed approval request emails for {RequestId} - sent to {Count} approvers", 
                eventData.ApprovalRequestId, approvers.Count(u => !string.IsNullOrEmpty(u.Email)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process approval request email job for {RequestId}", eventData.ApprovalRequestId);
        }
    }

    private static async Task<List<ApplicationUser>> GetUsersWithApprovalPermissionAsync(
        UserManager<ApplicationUser> userManager, 
        RoleManager<ApplicationRole> roleManager)
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

        // Get all users who have any of these roles in optimized queries
        var usersWithApprovalPermission = new List<ApplicationUser>();
        
        foreach (var roleName in rolesWithApprovalPermission)
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
            foreach (var user in usersInRole)
            {
                if (usersWithApprovalPermission.All(u => u.Id != user.Id))
                {
                    usersWithApprovalPermission.Add(user);
                }
            }
        }
        
        return usersWithApprovalPermission;
    }

    private async Task ProcessApprovalDecisionMadeAsync(ApprovalDecisionMadeEvent eventData, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            logger.LogInformation("Processing approval decision email job for {RequestId}", eventData.ApprovalRequestId);

            var decision = eventData.Decision == ApprovalStatus.Approved ? "Approved" : "Rejected";

            await emailService.SendApprovalDecisionNotificationAsync(
                eventData.RequesterEmail,
                eventData.RequesterName,
                eventData.DocumentName,
                decision,
                eventData.DecisionComment,
                eventData.ShareCode,
                eventData.ShareUrl
            );

            logger.LogInformation("Successfully sent approval decision email to requester for request {RequestId}", 
                eventData.ApprovalRequestId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process approval decision email job for {RequestId}", eventData.ApprovalRequestId);
        }
    }
} 