using System.Text.Json;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.DTOs;
using Qutora.Shared.Enums;
using Qutora.Shared.Events;

namespace Qutora.Application.Services;

/// <summary>
/// Refactored service for document sharing operations
/// Approval logic moved to DocumentShareApprovalService  
/// Notification logic moved to DocumentShareNotificationService
/// Creation logic refactored into smaller private methods
/// </summary>
public class DocumentShareService(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<DocumentShareService> logger,
    ITransactionManager transactionManager,
    IMapper mapper,
    IPasswordHashingService passwordHashingService,
    IApprovalPolicyService approvalPolicyService,
    IApprovalSettingsService approvalSettingsService,
    IEventPublisher eventPublisher,
    UserManager<ApplicationUser> userManager)
    : IDocumentShareService
{
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Creates a new document share with full validation and workflow
    /// </summary>
    public async Task<DocumentShareDto> CreateShareAsync(DocumentShareCreateDto shareDto,
        CancellationToken cancellationToken = default)
    {
        if (shareDto == null)
            throw new ArgumentNullException(nameof(shareDto));

        try
        {
            return await transactionManager.ExecuteInTransactionAsync(async () =>
            {
                // Step 1: Validate document exists
                var document = await ValidateDocumentExistsAsync(shareDto.DocumentId, cancellationToken);
                
                // Step 2: Generate unique share code
                var shareCode = await GenerateUniqueShareCodeAsync(cancellationToken);
                
                // Step 3: Create share entity
                var documentShare = CreateDocumentShareEntity(shareDto, shareCode);
                await unitOfWork.DocumentShares.AddAsync(documentShare, cancellationToken);

                // Step 4: Handle approval workflow
                await HandleDirectAccessAndApprovalAsync(documentShare, document, shareDto, cancellationToken);

                // Step 5: Save changes
                await unitOfWork.SaveChangesAsync(cancellationToken);
                
                // Step 6: Send notifications if needed
                await HandleShareNotificationsAsync(documentShare, shareDto, cancellationToken);

                return MapToDto(documentShare, document);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating document share for document {DocumentId}", shareDto.DocumentId);
            throw;
        }
    }

    /// <summary>
    /// Validates that document exists and returns it
    /// </summary>
    private async Task<Document> ValidateDocumentExistsAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
            throw new KeyNotFoundException($"Document with ID {documentId} not found");
        return document;
    }

    /// <summary>
    /// Creates DocumentShare entity from DTO
    /// </summary>
    private DocumentShare CreateDocumentShareEntity(DocumentShareCreateDto shareDto, string shareCode)
    {
        return new DocumentShare
        {
            Id = Guid.NewGuid(),
            DocumentId = shareDto.DocumentId,
            ShareCode = shareCode,
            CreatedBy = currentUserService.UserId ?? throw new InvalidOperationException("User ID is required"),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = shareDto.ExpiresAfterDays.HasValue
                ? DateTime.UtcNow.AddDays(shareDto.ExpiresAfterDays.Value)
                : null,
            IsActive = true,
            IsPasswordProtected = !string.IsNullOrWhiteSpace(shareDto.Password),
            PasswordHash = !string.IsNullOrWhiteSpace(shareDto.Password)
                ? passwordHashingService.HashPassword(shareDto.Password)
                : null,
            AllowDownload = shareDto.AllowDownload,
            AllowPrint = shareDto.AllowPrint,
            MaxViewCount = shareDto.MaxViewCount,
            WatermarkText = shareDto.WatermarkText,
            ShowWatermark = shareDto.ShowWatermark,
            CustomMessage = shareDto.CustomMessage,
            NotifyOnAccess = shareDto.NotifyOnAccess,
            NotificationEmails = shareDto.NotificationEmails?.Count > 0
                ? JsonSerializer.Serialize(shareDto.NotificationEmails)
                : null,
            IsDirectShare = shareDto.IsDirectShare
        };
    }
 


    /// <summary>
    /// Handles DirectAccess validation and approval logic
    /// </summary>
    private async Task HandleDirectAccessAndApprovalAsync(DocumentShare documentShare, Document document, 
        DocumentShareCreateDto shareDto, CancellationToken cancellationToken)
    {
        // DirectAccess validation
        var directAccessResult = await ValidateDirectAccessAsync(document, shareDto.IsDirectShare, cancellationToken);
        
        // Approval logic
        var approvalResult = await DetermineApprovalRequirementAsync(documentShare, directAccessResult, cancellationToken);
        
        if (approvalResult.RequiresApproval)
        {
            await SetupApprovalWorkflowAsync(documentShare, document, approvalResult, cancellationToken);
        }
        else
        {
            await SetupNormalShareAsync(documentShare, shareDto, cancellationToken);
        }
    }

    /// <summary>
    /// Validates DirectAccess permissions
    /// </summary>
    private async Task<(bool RequiresApproval, string Reason)> ValidateDirectAccessAsync(Document document, 
        bool isDirectShare, CancellationToken cancellationToken)
    {
        if (!isDirectShare)
            return (false, "");

        bool bucketAllowsDirectAccess = false;
        bool categoryAllowsDirectAccess = false;
        
        if (document.BucketId.HasValue)
        {
            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(document.BucketId.Value, cancellationToken);
            bucketAllowsDirectAccess = bucket?.AllowDirectAccess == true;
        }

        if (document.CategoryId.HasValue)
        {
            var category = await unitOfWork.Categories.GetByIdAsync(document.CategoryId.Value, cancellationToken);
            categoryAllowsDirectAccess = category?.AllowDirectAccess == true;
        }

        if (!bucketAllowsDirectAccess || !categoryAllowsDirectAccess)
        {
            logger.LogWarning(
                "Direct share requested but permissions denied. DocumentId: {DocumentId}, Bucket allows: {BucketAllows}, Category allows: {CategoryAllows}",
                document.Id, bucketAllowsDirectAccess, categoryAllowsDirectAccess);
            throw new InvalidOperationException("Direct share not allowed. Both bucket and category must have AllowDirectAccess enabled.");
        }

        logger.LogInformation("Direct share validation passed for document {DocumentId}", document.Id);
        return (true, "Direct share requires approval");
    }

    /// <summary>
    /// Determines if approval is required
    /// </summary>
    private async Task<(bool RequiresApproval, ApprovalPolicy? Policy, string Reason)> DetermineApprovalRequirementAsync(
        DocumentShare documentShare, (bool RequiresApproval, string Reason) directAccessResult, CancellationToken cancellationToken)
    {
        // DirectAccess always requires approval
        if (directAccessResult.RequiresApproval)
        {
            var defaultPolicy = await approvalSettingsService.EnsureDefaultPolicyExistsAsync(cancellationToken);
            return (true, defaultPolicy, directAccessResult.Reason);
        }

        // Check global approval settings
        var isGlobalApprovalEnabled = await approvalSettingsService.IsGlobalApprovalEnabledAsync(cancellationToken);
        if (!isGlobalApprovalEnabled)
        {
            return (false, null, "Global approval system is disabled");
        }

        // Check mandatory approval settings
        var requiresApprovalBySettings = await approvalSettingsService.RequiresApprovalAsync(documentShare, cancellationToken);
        if (requiresApprovalBySettings)
        {
            var defaultPolicy = await approvalSettingsService.EnsureDefaultPolicyExistsAsync(cancellationToken);
            return (true, defaultPolicy, "Required by approval settings (ForceApprovalForAll/ForceApprovalForLargeFiles)");
        }

        // Check specific policies
        var applicablePolicy = await approvalPolicyService.GetApplicablePolicyAsync(documentShare, cancellationToken);
        if (applicablePolicy != null)
        {
            return (true, applicablePolicy, $"Required by approval policy: {applicablePolicy.Name}");
        }

        return (false, null, "No approval rules or policies matched this share");
    }

    /// <summary>
    /// Sets up approval workflow for shares requiring approval
    /// </summary>
    private async Task SetupApprovalWorkflowAsync(DocumentShare documentShare, Document document, 
        (bool RequiresApproval, ApprovalPolicy? Policy, string Reason) approvalResult, CancellationToken cancellationToken)
    {
        documentShare.RequiresApproval = true;
        documentShare.ApprovalStatus = ApprovalStatus.Pending;
        documentShare.IsActive = false;

        logger.LogInformation("Document share {ShareId} requires approval: {Reason}", 
            documentShare.Id, approvalResult.Reason);

        if (approvalResult.Policy != null)
        {
            await CreateApprovalRequestAsync(documentShare, document, approvalResult.Policy, approvalResult.Reason, cancellationToken);
        }
    }

    /// <summary>
    /// Sets up normal share (no approval required)
    /// </summary>
    private async Task SetupNormalShareAsync(DocumentShare documentShare, DocumentShareCreateDto shareDto, CancellationToken cancellationToken)
    {
        documentShare.RequiresApproval = false;
        documentShare.ApprovalStatus = ApprovalStatus.NotRequired;
        documentShare.IsActive = true;

        logger.LogInformation("Document share {ShareId} does not require approval", documentShare.Id);

        // Publish event for email notifications (for both normal and DirectAccess shares)
        if (shareDto.NotificationEmails?.Any() == true)
        {
            try
            {
                var shareCreatedEvent = new DocumentShareCreatedEvent
                {
                    ShareId = documentShare.Id,
                    DocumentId = documentShare.DocumentId,
                    ShareCode = documentShare.ShareCode,
                    CreatedAt = documentShare.CreatedAt,
                    NotificationEmails = shareDto.NotificationEmails.ToArray(),
                    IsDirectShare = shareDto.IsDirectShare,
                    DocumentName = "Unknown", // Document name will be resolved by email service
                    CreatedByUserId = documentShare.CreatedBy
                };

                await eventPublisher.PublishAsync(shareCreatedEvent, cancellationToken);
                logger.LogInformation("Published DocumentShareCreated event for {ShareId} (DirectShare: {IsDirectShare})", 
                    documentShare.Id, shareDto.IsDirectShare);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish DocumentShareCreated event for {ShareId}", documentShare.Id);
            }
        }
    }

    /// <summary>
    /// Creates approval request and sends notifications
    /// </summary>
    private async Task CreateApprovalRequestAsync(DocumentShare documentShare, Document document, 
        ApprovalPolicy policy, string approvalReason, CancellationToken cancellationToken)
    {
        try
        {
            // AssignedApprovers will be populated by ApprovalEmailService
            var assignedApproversStr = ""; // Will be populated by email service when needed

            var approvalRequest = new ShareApprovalRequest
            {
                Id = Guid.NewGuid(),
                DocumentShareId = documentShare.Id,
                ApprovalPolicyId = policy.Id,
                Status = ApprovalStatus.Pending,
                RequestReason = approvalReason,
                RequestedByUserId = documentShare.CreatedBy,
                RequiredApprovalCount = policy.RequiredApprovalCount,
                CurrentApprovalCount = 0,
                AssignedApprovers = assignedApproversStr,
                ExpiresAt = DateTime.UtcNow.AddHours(policy.ApprovalTimeoutHours),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await unitOfWork.ShareApprovalRequests.AddAsync(approvalRequest, cancellationToken);

            var history = new ApprovalHistory
            {
                Id = Guid.NewGuid(),
                ShareApprovalRequestId = approvalRequest.Id,
                Action = ApprovalAction.Requested,
                ActionByUserId = documentShare.CreatedBy,
                ActionDate = DateTime.UtcNow,
                Notes = approvalReason
            };

            await unitOfWork.ApprovalHistories.AddAsync(history, cancellationToken);

            logger.LogInformation("Auto-created approval request {RequestId} for share {ShareId} using policy {PolicyId}",
                approvalRequest.Id, documentShare.Id, policy.Id);

            // Publish event for email job processing
            await PublishApprovalRequestCreatedEventAsync(approvalRequest, documentShare, document, policy, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to auto-create approval request for share {ShareId}. Share will remain inactive.", documentShare.Id);
        }
    }

    /// <summary>
    /// Publishes approval request created event for email job processing
    /// </summary>
    private async Task PublishApprovalRequestCreatedEventAsync(ShareApprovalRequest approvalRequest, DocumentShare documentShare, 
        Document document, ApprovalPolicy policy, CancellationToken cancellationToken)
    {
        try
        {
            // Get requester info
            var requester = await userManager.FindByIdAsync(approvalRequest.RequestedByUserId);
            var requesterName = requester != null ? $"{requester.FirstName} {requester.LastName}".Trim() : "Unknown User";

            // Get category name
            string categoryName = "Uncategorized";
            if (document.CategoryId.HasValue)
            {
                var category = await unitOfWork.Categories.GetByIdAsync(document.CategoryId.Value, cancellationToken);
                categoryName = category?.Name ?? "Uncategorized";
            }

            var eventData = new ApprovalRequestCreatedEvent
            {
                ApprovalRequestId = approvalRequest.Id,
                DocumentShareId = documentShare.Id,
                DocumentId = document.Id,
                DocumentName = document.Name,
                RequesterUserId = approvalRequest.RequestedByUserId,
                RequesterName = requesterName,
                ShareCode = documentShare.ShareCode,
                RequestReason = approvalRequest.RequestReason ?? "No reason provided",
                ExpiresAt = approvalRequest.ExpiresAt,
                CategoryName = categoryName,
                FileSizeBytes = document.FileSizeBytes,
                PolicyName = policy.Name
            };

            await eventPublisher.PublishAsync(eventData, cancellationToken);
            
            logger.LogInformation("Published ApprovalRequestCreated event for {RequestId}", approvalRequest.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish ApprovalRequestCreated event for {RequestId}", approvalRequest.Id);
        }
    }

        /// <summary>
    /// Handles share notifications via event bus
    /// </summary>
    private async Task HandleShareNotificationsAsync(DocumentShare documentShare, DocumentShareCreateDto shareDto, CancellationToken cancellationToken)
    {
        // Publish event for email notifications (for both normal and DirectAccess shares)
        if (shareDto.NotificationEmails?.Any() == true)
        {
            try
            {
                var shareCreatedEvent = new DocumentShareCreatedEvent
                {
                    ShareId = documentShare.Id,
                    DocumentId = documentShare.DocumentId,
                    ShareCode = documentShare.ShareCode,
                    CreatedAt = documentShare.CreatedAt,
                    NotificationEmails = shareDto.NotificationEmails.ToArray(),
                    IsDirectShare = shareDto.IsDirectShare,
                    DocumentName = "Unknown", // Document name will be resolved by email service
                    CreatedByUserId = documentShare.CreatedBy
                };

                await eventPublisher.PublishAsync(shareCreatedEvent, cancellationToken);
                logger.LogInformation("Published DocumentShareCreated event for {ShareId} (DirectShare: {IsDirectShare})", 
                    documentShare.Id, shareDto.IsDirectShare);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish DocumentShareCreated event for {ShareId}", documentShare.Id);
            }
        }
    }

    /// <summary>
    /// Updates a document share
    /// </summary>
    public async Task<DocumentShareDto?> UpdateShareAsync(Guid id, DocumentShareCreateDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (updateDto == null)
            throw new ArgumentNullException(nameof(updateDto));

        try
        {
            return await transactionManager.ExecuteInTransactionAsync(async () =>
            {
                var share = await unitOfWork.DocumentShares.GetByIdAsync(id, cancellationToken);
                if (share == null)
                    return null;

                var document = await unitOfWork.Documents.GetByIdAsync(share.DocumentId, cancellationToken);
                if (document == null)
                    return null;

                share.ExpiresAt = updateDto.ExpiresAfterDays.HasValue
                    ? DateTime.UtcNow.AddDays(updateDto.ExpiresAfterDays.Value)
                    : null;

                share.IsPasswordProtected = !string.IsNullOrWhiteSpace(updateDto.Password);
                if (!string.IsNullOrWhiteSpace(updateDto.Password))
                    share.PasswordHash = passwordHashingService.HashPassword(updateDto.Password);
                else
                    share.PasswordHash = null;

                share.AllowDownload = updateDto.AllowDownload;
                share.AllowPrint = updateDto.AllowPrint;
                share.MaxViewCount = updateDto.MaxViewCount;
                share.WatermarkText = updateDto.WatermarkText;
                share.ShowWatermark = updateDto.ShowWatermark;
                share.CustomMessage = updateDto.CustomMessage;
                share.NotifyOnAccess = updateDto.NotifyOnAccess;
                share.NotificationEmails = updateDto.NotificationEmails?.Count > 0
                    ? JsonSerializer.Serialize(updateDto.NotificationEmails)
                    : null;

                await unitOfWork.SaveChangesAsync(cancellationToken);

                return MapToDto(share, document);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating document share {ShareId}", id);
            throw;
        }
    }

    /// <summary>
    /// Validates password for password-protected shares
    /// </summary>
    public async Task<ShareAccessInfoDto> ValidateSharePasswordAsync(SharePasswordValidationDto validationDto,
        string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        if (validationDto == null)
            throw new ArgumentNullException(nameof(validationDto));

        try
        {
            var share = await unitOfWork.DocumentShares.GetByShareCodeAsync(validationDto.ShareCode,
                cancellationToken);
            if (share == null)
                return new ShareAccessInfoDto
                {
                    IsValid = false,
                    ErrorMessage = "Share not found."
                };

            if (!share.IsPasswordProtected || string.IsNullOrWhiteSpace(share.PasswordHash))
                return new ShareAccessInfoDto
                {
                    IsValid = false,
                    ErrorMessage = "This share is not password protected."
                };

            if (!passwordHashingService.VerifyPassword(validationDto.Password, share.PasswordHash))
                return new ShareAccessInfoDto
                {
                    IsValid = false,
                    ErrorMessage = "Incorrect password."
                };

            return await PerformAccessChecks(share, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating share password for code {ShareCode}", validationDto.ShareCode);
            throw;
        }
    }

    /// <summary>
    /// Gets share access information
    /// </summary>
    public async Task<ShareAccessInfoDto> GetShareAccessInfoAsync(string shareCode, string ipAddress, string userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareCode))
            throw new ArgumentException("Share code cannot be null or empty", nameof(shareCode));

        try
        {
            var share = await unitOfWork.DocumentShares.GetByShareCodeAsync(shareCode, cancellationToken);
            if (share == null)
                return new ShareAccessInfoDto
                {
                    IsValid = false,
                    ErrorMessage = "Share not found."
                };

            if (share.IsPasswordProtected)
                return new ShareAccessInfoDto
                {
                    IsValid = false,
                    RequiresPassword = true,
                    ErrorMessage = "This share is password protected."
                };

            return await PerformAccessChecks(share, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting share access info for code {ShareCode}", shareCode);
            throw;
        }
    }

    /// <summary>
    /// Performs access checks for shares
    /// </summary>
    private async Task<ShareAccessInfoDto> PerformAccessChecks(DocumentShare share,
        CancellationToken cancellationToken = default)
    {
        if (!share.IsActive)
            return new ShareAccessInfoDto
            {
                IsValid = false,
                ErrorMessage = "This share has been disabled."
            };

        if (share.ExpiresAt.HasValue && share.ExpiresAt.Value < DateTime.UtcNow)
            return new ShareAccessInfoDto
            {
                IsValid = false,
                ErrorMessage = "This share has expired."
            };

        if (share.IsViewLimitReached)
            return new ShareAccessInfoDto
            {
                IsValid = false,
                IsViewLimitReached = true,
                ErrorMessage = "This share has reached its maximum view count."
            };

        return new ShareAccessInfoDto
        {
            IsValid = true,
            DocumentId = share.DocumentId,
            ShareId = share.Id,
            AllowDownload = share.AllowDownload,
            AllowPrint = share.AllowPrint,
            WatermarkText = share.WatermarkText,
            ShowWatermark = share.ShowWatermark,
            CustomMessage = share.CustomMessage,
            IsViewLimitReached = share.IsViewLimitReached,
            RemainingViews = share.MaxViewCount.HasValue
                ? Math.Max(0, share.MaxViewCount.Value - share.ViewCount)
                : null
        };
    }

    /// <summary>
    /// Records a share view
    /// </summary>
    public async Task<bool> RecordShareViewAsync(Guid shareId, string ipAddress, string userAgent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                var share = await unitOfWork.DocumentShares.GetByIdAsync(shareId, cancellationToken);
                if (share == null)
                    return false;

                if (share.IsViewLimitReached)
                {
                    logger.LogWarning("Attempt to view share {ShareId} that has reached view limit", shareId);
                    return false;
                }

                var incrementResult =
                    await unitOfWork.DocumentShares.IncrementViewCountAsync(shareId, cancellationToken);
                if (!incrementResult)
                    return false;

                var view = new DocumentShareView
                {
                    Id = Guid.NewGuid(),
                    ShareId = shareId,
                    ViewedAt = DateTime.UtcNow,
                    ViewerIP = ipAddress ?? "unknown",
                    UserAgent = userAgent ?? "unknown"
                };

                await unitOfWork.DocumentShareViews.AddAsync(view, cancellationToken);

                // Access notifications removed - only share creation and approval emails will be sent

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording share view for share {ShareId}", shareId);
            throw;
        }
    }



    /// <summary>
    /// Maps DocumentShare entity to DTO
    /// </summary>
    private DocumentShareDto MapToDto(DocumentShare share, Document document)
    {
        var notificationEmails = new List<string>();
        if (!string.IsNullOrWhiteSpace(share.NotificationEmails))
            try
            {
                notificationEmails = JsonSerializer.Deserialize<List<string>>(share.NotificationEmails) ??
                                     [];
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Error deserializing notification emails for share {ShareId}", share.Id);
            }

        return new DocumentShareDto
        {
            Id = share.Id,
            DocumentId = share.DocumentId,
            DocumentName = document.FileName,
            ShareCode = share.ShareCode,
            CreatedBy = share.CreatedBy,
            CreatedByName = share.CreatedBy,
            CreatedAt = share.CreatedAt,
            ExpiresAt = share.ExpiresAt,
            ViewCount = share.ViewCount,
            IsActive = share.IsActive,
            ShareUrl = $"/document/{share.ShareCode}",

            IsPasswordProtected = share.IsPasswordProtected,
            AllowDownload = share.AllowDownload,
            AllowPrint = share.AllowPrint,
            MaxViewCount = share.MaxViewCount,
            WatermarkText = share.WatermarkText,
            ShowWatermark = share.ShowWatermark,
            CustomMessage = share.CustomMessage,
            NotifyOnAccess = share.NotifyOnAccess,
            NotificationEmails = notificationEmails,

            RequiresApproval = share.RequiresApproval,
            ApprovalStatus = share.ApprovalStatus,
            IsDirectShare = share.IsDirectShare,
            FileExtension = !string.IsNullOrEmpty(document.FileName) && document.FileName.Contains('.')
                ? document.FileName.Substring(document.FileName.LastIndexOf('.'))
                : string.Empty
        };
    }

    /// <summary>
    /// Gets document share by ID
    /// </summary>
    public async Task<DocumentShareDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var share = await unitOfWork.DocumentShares.GetByIdAsync(id, cancellationToken);
            if (share == null)
                return null;

            var document = await unitOfWork.Documents.GetByIdAsync(share.DocumentId, cancellationToken);
            if (document == null)
                return null;

            return MapToDto(share, document);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document share with ID {ShareId}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets document share by share code
    /// </summary>
    public async Task<DocumentShareDto?> GetByShareCodeAsync(string shareCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(shareCode))
            throw new ArgumentException("Share code cannot be null or empty", nameof(shareCode));

        try
        {
            var share = await unitOfWork.DocumentShares.GetByShareCodeAsync(shareCode, cancellationToken);
            if (share == null)
                return null;

            var document = await unitOfWork.Documents.GetByIdAsync(share.DocumentId, cancellationToken);
            if (document == null)
                return null;

            return MapToDto(share, document);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document share with code {ShareCode}", shareCode);
            throw;
        }
    }

    /// <summary>
    /// Gets shares for a document
    /// </summary>
    public async Task<IEnumerable<DocumentShareDto>> GetByDocumentIdAsync(Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var shares = await unitOfWork.DocumentShares.GetByDocumentIdAsync(documentId, cancellationToken);
            var document = await unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
            if (document == null)
                return [];

            return shares.Select(share => MapToDto(share, document));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document shares for document {DocumentId}", documentId);
            throw;
        }
    }

    /// <summary>
    /// Gets current user's shares
    /// </summary>
    public async Task<IEnumerable<DocumentShareDto>> GetMySharesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserId))
                throw new InvalidOperationException("Current user ID is required");

            var shares = await unitOfWork.DocumentShares.GetByUserIdAsync(currentUserId, cancellationToken);

            var sharesDtos = new List<DocumentShareDto>();
            foreach (var share in shares)
            {
                var document = await unitOfWork.Documents.GetByIdAsync(share.DocumentId, cancellationToken);
                if (document != null)
                {
                    sharesDtos.Add(MapToDto(share, document));
                }
            }

            return sharesDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user shares");
            throw;
        }
    }

    public async Task<PagedDto<DocumentShareDto>> GetUserSharesPagedAsync(string userId, int page = 1, int pageSize = 10, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var allShares = await unitOfWork.DocumentShares.GetByUserIdAsync(userId, cancellationToken);
            var query = allShares.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(s => s.ShareCode.ToLower().Contains(searchLower) ||
                                       (s.CustomMessage != null && s.CustomMessage.ToLower().Contains(searchLower)));
            }

            var totalCount = query.Count();
            var shares = query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var sharesDtos = new List<DocumentShareDto>();
            foreach (var share in shares)
            {
                var document = await unitOfWork.Documents.GetByIdAsync(share.DocumentId, cancellationToken);
                if (document != null)
                {
                    sharesDtos.Add(MapToDto(share, document));
                }
            }

            return new PagedDto<DocumentShareDto>
            {
                Items = sharesDtos,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paged user shares for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Completely deletes document share (hard delete)
    /// </summary>
    public async Task<bool> DeleteShareAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await transactionManager.ExecuteInTransactionAsync(async () =>
            {
                var share = await unitOfWork.DocumentShares.GetByIdAsync(id, cancellationToken);
                if (share == null)
                    return false;

                var views = await unitOfWork.DocumentShareViews.GetByShareIdAsync(id, cancellationToken);
                foreach (var view in views) unitOfWork.DocumentShareViews.Remove(view);

                unitOfWork.DocumentShares.Remove(share);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return true;
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document share {ShareId}", id);
            throw;
        }
    }

    /// <summary>
    /// Changes document share status (active/inactive)
    /// </summary>
    public async Task<bool> ToggleShareStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await transactionManager.ExecuteInTransactionAsync(async () =>
            {
                var share = await unitOfWork.DocumentShares.GetByIdAsync(id, cancellationToken);
                if (share == null)
                    return false;

                share.IsActive = !share.IsActive;
                await unitOfWork.DocumentShares.UpdateAsync(share, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                return true;
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling document share status {ShareId}", id);
            throw;
        }
    }

    /// <summary>
    /// Generates unique share code
    /// </summary>
    private async Task<string> GenerateUniqueShareCodeAsync(CancellationToken cancellationToken = default)
    {
        var random = new Random();
        const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        while (true)
        {
            var code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            var exists = await unitOfWork.DocumentShares.GetByShareCodeAsync(code, cancellationToken) != null;
            if (!exists)
                return code;
        }
    }

    /// <summary>
    /// Gets user share view trend data for charts
    /// </summary>
    public async Task<DocumentShareTrendDataDto> GetUserShareViewTrendAsync(string userId, int monthCount = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID is required", nameof(userId));

            var userShares = await unitOfWork.DocumentShares.GetByUserIdAsync(userId, cancellationToken);
            
            var monthlyData = new List<DocumentShareViewTrendDto>();
            var now = DateTime.UtcNow;
            
            for (int i = monthCount - 1; i >= 0; i--)
            {
                var targetDate = now.AddMonths(-i);
                var monthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                
                var monthViews = userShares
                    .Where(s => s.CreatedAt >= monthStart && s.CreatedAt <= monthEnd)
                    .Sum(s => s.ViewCount);
                
                monthlyData.Add(new DocumentShareViewTrendDto
                {
                    Month = targetDate.ToString("MMM"),
                    Year = targetDate.Year,
                    ViewCount = monthViews
                });
            }
            
            var totalViews = userShares.Sum(s => s.ViewCount);
            var averageViews = monthlyData.Count > 0 ? (double)totalViews / monthlyData.Count : 0;
            
            return new DocumentShareTrendDataDto
            {
                MonthlyViews = monthlyData,
                TotalViews = totalViews,
                AverageViewsPerMonth = Math.Round(averageViews, 1)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user share view trend for user {UserId}", userId);
            throw;
        }
    }
}
