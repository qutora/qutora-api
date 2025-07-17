using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Approval;
using Qutora.Shared.Enums;
using Qutora.Shared.Events;
using Qutora.Shared.Exceptions;

namespace Qutora.Application.Services;

public class ApprovalService(
    IUnitOfWork unitOfWork,
    ILogger<ApprovalService> logger,
    IApprovalPolicyService policyService,
    IEmailService emailService,
    UserManager<ApplicationUser> userManager,
    IApprovalEmailService approvalEmailService,
    IEventPublisher eventPublisher,
    IConfiguration configuration)
    : IApprovalService
{
    private readonly IEmailService _emailService = emailService;

    public async Task<ShareApprovalRequestDto> CreateApprovalRequestAsync(
        Guid documentShareId,
        Guid policyId,
        string? requestReason = null,
        CancellationToken cancellationToken = default)
    {
        var result = await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var documentShare = await unitOfWork.DocumentShares.GetByIdAsync(documentShareId, cancellationToken);
            if (documentShare == null)
                throw new InvalidOperationException($"Document share {documentShareId} not found");

            var policy = await unitOfWork.ApprovalPolicies.GetByIdAsync(policyId, cancellationToken);
            if (policy == null) throw new InvalidOperationException($"Approval policy {policyId} not found");

            var assignedApprovers =
                await policyService.GetAssignedApproversAsync(policy, documentShare, cancellationToken);

            var approvalRequest = new ShareApprovalRequest
            {
                Id = Guid.NewGuid(),
                DocumentShareId = documentShareId,
                ApprovalPolicyId = policyId,
                Status = ApprovalStatus.Pending,
                RequestReason = requestReason,
                RequestedByUserId = documentShare.CreatedBy,
                RequiredApprovalCount = policy.RequiredApprovalCount,
                CurrentApprovalCount = 0,
                AssignedApprovers = string.Join(",", assignedApprovers),
                ExpiresAt = DateTime.UtcNow.AddHours(policy.ApprovalTimeoutHours),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await unitOfWork.ShareApprovalRequests.AddAsync(approvalRequest, cancellationToken);

            documentShare.RequiresApproval = true;
            documentShare.ApprovalStatus = ApprovalStatus.Pending;
            unitOfWork.DocumentShares.Update(documentShare);

            var history = new ApprovalHistory
            {
                Id = Guid.NewGuid(),
                ShareApprovalRequestId = approvalRequest.Id,
                Action = ApprovalAction.Requested,
                ActionByUserId = documentShare.CreatedBy,
                ActionDate = DateTime.UtcNow,
                Notes = requestReason ?? "Approval request created"
            };

            await unitOfWork.ApprovalHistories.AddAsync(history, cancellationToken);

            logger.LogInformation("Approval request created: {RequestId} for document share {ShareId}",
                approvalRequest.Id, documentShareId);

            return approvalRequest.Adapt<ShareApprovalRequestDto>();
        }, cancellationToken);

        _ = Task.Run(async () =>
        {
            try
            {
                await approvalEmailService.SendApprovalRequestEmailsAsync(result.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send approval request notifications for {RequestId}", result.Id);
            }
        }, cancellationToken);

        return result;
    }

    public async Task<ApprovalResultDto> ProcessApprovalAsync(
        Guid approvalRequestId,
        ApprovalAction decision,
        string? comment = null,
        string? approverUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                var request =
                    await unitOfWork.ShareApprovalRequests.GetByIdAsync(approvalRequestId, cancellationToken);
                if (request == null)
                    throw new InvalidOperationException($"Approval request {approvalRequestId} not found");

                if (request.Status != ApprovalStatus.Pending)
                    throw new InvalidOperationException($"Approval request {approvalRequestId} is not pending");

                if (request.ExpiresAt <= DateTime.UtcNow)
                    throw new InvalidOperationException($"Approval request {approvalRequestId} has expired");

                var approvalDecision = new ApprovalDecision
                {
                    Id = Guid.NewGuid(),
                    ShareApprovalRequestId = approvalRequestId,
                    ApproverUserId = approverUserId ?? "UNKNOWN",
                    Decision = decision,
                    Comment = comment,
                    ApprovedAt = DateTime.UtcNow
                };

                await unitOfWork.ApprovalDecisions.AddAsync(approvalDecision, cancellationToken);

                var history = new ApprovalHistory
                {
                    Id = Guid.NewGuid(),
                    ShareApprovalRequestId = approvalRequestId,
                    Action = decision,
                    ActionByUserId = approverUserId ?? "UNKNOWN",
                    ActionDate = DateTime.UtcNow,
                    Notes = comment ?? $"Request {decision.ToString().ToLower()}"
                };

                await unitOfWork.ApprovalHistories.AddAsync(history, cancellationToken);

                if (decision == ApprovalAction.Approved)
                {
                    request.CurrentApprovalCount++;
                    if (request.CurrentApprovalCount >= request.RequiredApprovalCount)
                    {
                        request.Status = ApprovalStatus.Approved;
                        request.ProcessedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    request.Status = ApprovalStatus.Rejected;
                    request.ProcessedAt = DateTime.UtcNow;
                }

                request.FinalComment = comment;
                request.UpdatedAt = DateTime.UtcNow;
                unitOfWork.ShareApprovalRequests.Update(request);

                var documentShare =
                    await unitOfWork.DocumentShares.GetByIdAsync(request.DocumentShareId, cancellationToken);
                if (documentShare != null)
                {
                    documentShare.ApprovalStatus = request.Status;

                    switch (request.Status)
                    {
                        case ApprovalStatus.Approved:
                            documentShare.IsActive = true;
                            break;
                        case ApprovalStatus.Rejected:
                        case ApprovalStatus.Expired:
                            documentShare.IsActive = false;
                            break;
                    }

                    unitOfWork.DocumentShares.Update(documentShare);

                }

                logger.LogInformation("Approval request {RequestId} {Action} by {ApproverId}",
                    approvalRequestId, decision, approverUserId);

                var result = new ApprovalResultDto
                {
                    RequiresApproval = true,
                    ApprovalRequestId = approvalRequestId,
                    Status = request.Status,
                    Message = $"Approval request {decision.ToString().ToLower()} successfully"
                };

                if (request.Status == ApprovalStatus.Approved || request.Status == ApprovalStatus.Rejected)
                {
                    await PublishApprovalDecisionEventAsync(request, cancellationToken);
                    
                    // If approved, also send the share notification to recipients
                    if (request.Status == ApprovalStatus.Approved && documentShare != null)
                    {
                        await PublishShareCreatedEventAfterApprovalAsync(documentShare, cancellationToken);
                    }
                }

                return result;
            }, cancellationToken);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while processing approval request {RequestId}",
                approvalRequestId);
            throw new InvalidOperationException(
                "Approval request was modified by another user. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing approval request {RequestId}", approvalRequestId);
            throw;
        }
    }

    public async Task<PagedDto<ShareApprovalRequestDto>> GetPendingApprovalsAsync(
        ApprovalRequestQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var allRequests = await unitOfWork.ShareApprovalRequests.GetPendingRequestsAsync(cancellationToken);

        var filteredRequests = allRequests.AsQueryable();

        if (!string.IsNullOrEmpty(query.RequesterUserId))
            filteredRequests = filteredRequests.Where(x => x.RequestedByUserId == query.RequesterUserId);

        if (query.Status.HasValue)
            filteredRequests = filteredRequests.Where(x => x.Status == query.Status.Value);

        if (query.RequestedAfter.HasValue)
            filteredRequests = filteredRequests.Where(x => x.CreatedAt >= query.RequestedAfter.Value);

        if (query.RequestedBefore.HasValue)
            filteredRequests = filteredRequests.Where(x => x.CreatedAt <= query.RequestedBefore.Value);

        var totalCount = filteredRequests.Count();

        var pagedItems = filteredRequests
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var dtos = pagedItems.Select(MapToDto).ToList();

        return new PagedDto<ShareApprovalRequestDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    public async Task<PagedDto<ShareApprovalRequestDto>> GetMyApprovalRequestsAsync(
        ApprovalRequestQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var allRequests = await unitOfWork.ShareApprovalRequests.GetAllAsync(cancellationToken);

        var filteredRequests = allRequests.AsQueryable();

        if (!string.IsNullOrEmpty(query.RequesterUserId))
            filteredRequests = filteredRequests.Where(x => x.RequestedByUserId == query.RequesterUserId);

        if (query.Status.HasValue)
            filteredRequests = filteredRequests.Where(x => x.Status == query.Status.Value);

        if (query.RequestedAfter.HasValue)
            filteredRequests = filteredRequests.Where(x => x.CreatedAt >= query.RequestedAfter.Value);

        if (query.RequestedBefore.HasValue)
            filteredRequests = filteredRequests.Where(x => x.CreatedAt <= query.RequestedBefore.Value);

        var totalCount = filteredRequests.Count();

        var pagedItems = filteredRequests
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var dtos = pagedItems.Select(MapToDto).ToList();

        return new PagedDto<ShareApprovalRequestDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    public async Task<ShareApprovalRequestDto?> GetRequestByIdAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        var request = await unitOfWork.ShareApprovalRequests.GetByIdAsync(approvalRequestId, cancellationToken);
        if (request == null)
            return null;

        return MapToDto(request);
    }

    public async Task<List<ApprovalHistoryDto>> GetApprovalHistoryAsync(
        Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        var historyEntries =
            await unitOfWork.ApprovalHistories.GetByApprovalRequestIdAsync(approvalRequestId, cancellationToken);
        return historyEntries.Adapt<List<ApprovalHistoryDto>>();
    }

    public async Task<ApprovalStatisticsDto> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        fromDate ??= DateTime.UtcNow.AddDays(-30);
        toDate ??= DateTime.UtcNow;

        var allRequests = await unitOfWork.ShareApprovalRequests.GetAllAsync(cancellationToken);
        var filteredRequests = allRequests.Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate).ToList();

        var statistics = new ApprovalStatisticsDto
        {
            TotalRequests = filteredRequests.Count,
            PendingRequests = filteredRequests.Count(x => x.Status == ApprovalStatus.Pending),
            ApprovedRequests = filteredRequests.Count(x => x.Status == ApprovalStatus.Approved),
            RejectedRequests = filteredRequests.Count(x => x.Status == ApprovalStatus.Rejected),
            ExpiredRequests = filteredRequests.Count(x => x.Status == ApprovalStatus.Expired),
            AverageApprovalTimeHours = filteredRequests
                .Where(x => x.ProcessedAt.HasValue)
                .Select(x => (x.ProcessedAt!.Value - x.CreatedAt).TotalHours)
                .DefaultIfEmpty(0)
                .Average()
        };

        return statistics;
    }

    public async Task<bool> CanUserApproveRequestAsync(
        Guid approvalRequestId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var request = await unitOfWork.ShareApprovalRequests.GetByIdAsync(approvalRequestId, cancellationToken);
        if (request == null)
            return false;

        if (request.Status != ApprovalStatus.Pending)
            return false;

        if (request.ExpiresAt <= DateTime.UtcNow)
            return false;

        var existingDecision =
            await unitOfWork.ApprovalDecisions.GetByApprovalRequestAndUserAsync(approvalRequestId, userId,
                cancellationToken);
        if (existingDecision != null)
            return false;

        if (!string.IsNullOrEmpty(request.AssignedApprovers)) return request.AssignedApprovers.Contains(userId);

        return true;
    }

    public async Task ProcessExpiredRequestsAsync(CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var expiredRequests = await unitOfWork.ShareApprovalRequests.GetExpiredRequestsAsync(cancellationToken);

            foreach (var request in expiredRequests)
            {
                request.Status = ApprovalStatus.Expired;
                request.ProcessedAt = DateTime.UtcNow;
                request.UpdatedAt = DateTime.UtcNow;
                unitOfWork.ShareApprovalRequests.Update(request);

                var history = new ApprovalHistory
                {
                    Id = Guid.NewGuid(),
                    ShareApprovalRequestId = request.Id,
                    Action = ApprovalAction.Expired,
                    ActionByUserId = "SYSTEM",
                    ActionDate = DateTime.UtcNow,
                    Notes = "Request expired automatically"
                };

                await unitOfWork.ApprovalHistories.AddAsync(history, cancellationToken);

                var documentShare =
                    await unitOfWork.DocumentShares.GetByIdAsync(request.DocumentShareId, cancellationToken);
                if (documentShare != null)
                {
                    documentShare.ApprovalStatus = ApprovalStatus.Expired;
                    documentShare.IsActive = false;
                    unitOfWork.DocumentShares.Update(documentShare);
                }
            }

            if (expiredRequests.Any())
            {
                logger.LogInformation("Processed {Count} expired approval requests", expiredRequests.Count());
            }
        }, cancellationToken);
    }

    private static ShareApprovalRequestDto MapToDto(ShareApprovalRequest request)
    {
        return new ShareApprovalRequestDto
        {
            Id = request.Id,
            DocumentShareId = request.DocumentShareId,
            DocumentName = request.DocumentShare?.Document?.Name ?? string.Empty,
            CategoryName = request.DocumentShare?.Document?.Category?.Name ?? string.Empty,
            ShareCode = request.DocumentShare?.ShareCode ?? string.Empty,
            ApprovalPolicyId = request.ApprovalPolicyId,
            PolicyName = request.ApprovalPolicy?.Name ?? string.Empty,
            Status = request.Status,
            RequestReason = request.RequestReason,
            FinalComment = request.FinalComment,
            RequestedByUserId = request.RequestedByUserId,
            RequestedByUserName = request.RequestedByUser?.UserName ?? string.Empty,
            RequesterName = request.RequestedByUser != null
                ? $"{request.RequestedByUser.FirstName} {request.RequestedByUser.LastName}".Trim()
                : string.Empty,
            RequiredApprovalCount = request.RequiredApprovalCount,
            CurrentApprovalCount = request.CurrentApprovalCount,
            AssignedApprovers = string.IsNullOrEmpty(request.AssignedApprovers)
                ? new List<string>()
                : request.AssignedApprovers.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            ExpiresAt = request.ExpiresAt,
            ProcessedAt = request.ProcessedAt,
            Priority = request.Priority,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }

    private async Task PublishApprovalDecisionEventAsync(ShareApprovalRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var documentShare = await unitOfWork.DocumentShares.GetByIdAsync(request.DocumentShareId, cancellationToken);
            if (documentShare?.Document == null)
            {
                logger.LogWarning("Document share or document not found for approval request {RequestId}", request.Id);
                return;
            }

            var requester = await userManager.FindByIdAsync(request.RequestedByUserId);
            if (requester == null)
            {
                logger.LogWarning("Requester not found for approval request {RequestId}", request.Id);
                return;
            }

            // Get share URL from configuration or use default
            var baseUrl = configuration?["PublicViewer:BaseUrl"] ?? "https://qutora.io";
            var shareUrl = $"{baseUrl.TrimEnd('/')}/share/{documentShare.ShareCode}";

            var decisionEvent = new ApprovalDecisionMadeEvent
            {
                ApprovalRequestId = request.Id,
                RequesterEmail = requester.Email!,
                RequesterName = $"{requester.FirstName} {requester.LastName}".Trim(),
                DocumentName = documentShare.Document.Name,
                Decision = request.Status,
                DecisionComment = request.FinalComment,
                ShareCode = documentShare.ShareCode,
                ShareUrl = shareUrl
            };

            await eventPublisher.PublishAsync(decisionEvent);
            logger.LogInformation("Published approval decision event for request {RequestId}", request.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish approval decision event for request {RequestId}", request.Id);
        }
    }

    /// <summary>
    /// Publishes DocumentShareCreated event after approval to send share notification to recipients
    /// </summary>
    private async Task PublishShareCreatedEventAfterApprovalAsync(DocumentShare documentShare, CancellationToken cancellationToken = default)
    {
        try
        {
            // Only send if there are notification emails
            if (string.IsNullOrWhiteSpace(documentShare.NotificationEmails))
                return;

            var emails = System.Text.Json.JsonSerializer.Deserialize<List<string>>(documentShare.NotificationEmails);
            if (emails == null || !emails.Any())
                return;

            // Get document details
            var document = await unitOfWork.Documents.GetByIdAsync(documentShare.DocumentId, cancellationToken);
            if (document == null)
                return;

            var shareCreatedEvent = new DocumentShareCreatedEvent
            {
                ShareId = documentShare.Id,
                DocumentId = documentShare.DocumentId,
                ShareCode = documentShare.ShareCode,
                CreatedAt = documentShare.CreatedAt,
                NotificationEmails = emails.ToArray(),
                IsDirectShare = documentShare.IsDirectShare,
                DocumentName = document.Name,
                CreatedByUserId = documentShare.CreatedBy
            };

            await eventPublisher.PublishAsync(shareCreatedEvent, cancellationToken);
            logger.LogInformation("Published DocumentShareCreated event after approval for share {ShareId} (DirectShare: {IsDirectShare})", 
                documentShare.Id, documentShare.IsDirectShare);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish DocumentShareCreated event after approval for share {ShareId}", documentShare.Id);
        }
    }

}