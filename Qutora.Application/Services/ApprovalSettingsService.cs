using Mapster;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities;
using Qutora.Shared.DTOs.Approval;
using Qutora.Shared.Exceptions;

namespace Qutora.Application.Services;

public class ApprovalSettingsService(
    IUnitOfWork unitOfWork,
    ILogger<ApprovalSettingsService> logger)
    : IApprovalSettingsService
{
    public async Task<ApprovalSettingsDto> GetCurrentSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return settings.Adapt<ApprovalSettingsDto>();
    }

    public async Task EnableGlobalApprovalAsync(
        string reason,
        string? adminUserId = null,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var settings = await GetOrCreateSettingsAsync(cancellationToken);

            settings.IsGlobalApprovalEnabled = true;
            settings.GlobalApprovalReason = reason;
            settings.GlobalApprovalEnabledAt = DateTime.UtcNow;
            settings.GlobalApprovalEnabledByUserId = adminUserId;
            settings.UpdatedAt = DateTime.UtcNow;

            unitOfWork.ApprovalSettings.Update(settings);

            logger.LogInformation("Global approval enabled by {AdminUserId}. Reason: {Reason}",
                adminUserId ?? "SYSTEM", reason);
        }, cancellationToken);
    }

    public async Task DisableGlobalApprovalAsync(
        string? adminUserId = null,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var settings = await GetOrCreateSettingsAsync(cancellationToken);

            settings.IsGlobalApprovalEnabled = false;
            settings.GlobalApprovalReason = null;
            settings.GlobalApprovalEnabledAt = null;
            settings.GlobalApprovalEnabledByUserId = null;
            settings.UpdatedAt = DateTime.UtcNow;

            unitOfWork.ApprovalSettings.Update(settings);

            logger.LogInformation("Global approval disabled by {AdminUserId}", adminUserId ?? "SYSTEM");
        }, cancellationToken);
    }

    public async Task<bool> IsGlobalApprovalEnabledAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return settings.IsGlobalApprovalEnabled;
    }

    public async Task<ApprovalSettingsDto> UpdateSettingsAsync(UpdateApprovalSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var settings = await GetOrCreateSettingsAsync(cancellationToken);


            settings.ForceApprovalForLargeFiles = dto.ForceApprovalForLargeFiles ?? settings.ForceApprovalForLargeFiles;
            settings.LargeFileSizeThresholdBytes =
                dto.LargeFileSizeThresholdBytes ?? settings.LargeFileSizeThresholdBytes;
            settings.DefaultExpirationDays = dto.DefaultExpirationDays ?? settings.DefaultExpirationDays;
            settings.DefaultRequiredApprovals = dto.DefaultRequiredApprovals ?? settings.DefaultRequiredApprovals;
            settings.EnableEmailNotifications = dto.EnableEmailNotifications ?? settings.EnableEmailNotifications;

            if (dto.IsGlobalApprovalEnabled.HasValue)
            {
                settings.IsGlobalApprovalEnabled = dto.IsGlobalApprovalEnabled.Value;
                if (dto.IsGlobalApprovalEnabled.Value)
                {
                    settings.GlobalApprovalEnabledAt = DateTime.UtcNow;
                    settings.GlobalApprovalReason = dto.GlobalApprovalReason;
                }
                else
                {
                    settings.GlobalApprovalEnabledAt = null;
                    settings.GlobalApprovalReason = null;
                    settings.GlobalApprovalEnabledByUserId = null;
                }
            }

            if (dto.ForceApprovalForAll.HasValue) settings.ForceApprovalForAll = dto.ForceApprovalForAll.Value;

            settings.UpdatedAt = DateTime.UtcNow;

            unitOfWork.ApprovalSettings.Update(settings);

            logger.LogInformation("Approval settings updated");

            return settings.Adapt<ApprovalSettingsDto>();
        }, cancellationToken);
    }

    public async Task<ApprovalSettingsDto> ResetSettingsToDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var settings = await GetOrCreateSettingsAsync(cancellationToken);

            settings.IsGlobalApprovalEnabled = false;
            settings.GlobalApprovalReason = null;
            settings.GlobalApprovalEnabledAt = null;
            settings.GlobalApprovalEnabledByUserId = null;
            settings.DefaultExpirationDays = 7;
            settings.DefaultRequiredApprovals = 1;
            settings.ForceApprovalForAll = false;
            settings.ForceApprovalForLargeFiles = true;
            settings.LargeFileSizeThresholdBytes = 100 * 1024 * 1024;
            settings.EnableEmailNotifications = true;
            settings.UpdatedAt = DateTime.UtcNow;

            unitOfWork.ApprovalSettings.Update(settings);

            logger.LogInformation("Approval settings reset to default values");

            return settings.Adapt<ApprovalSettingsDto>();
        }, cancellationToken);
    }

    public async Task<bool> IsApprovalRequiredAsync(Guid documentShareId, CancellationToken cancellationToken = default)
    {
        var documentShare = await unitOfWork.DocumentShares.GetByIdAsync(documentShareId, cancellationToken);
        if (documentShare == null)
            throw new EntityNotFoundException($"Document share with ID {documentShareId} not found.");

        var settings = await GetOrCreateSettingsAsync(cancellationToken);

        // CRITICAL: If global approval is disabled, no shares require approval regardless of other settings
        if (!settings.IsGlobalApprovalEnabled)
            return false;

        // Global approval is enabled, check specific rules
        if (settings.ForceApprovalForAll)
            return true;



        if (settings.ForceApprovalForLargeFiles)
        {
            var document = await unitOfWork.Documents.GetByIdAsync(documentShare.DocumentId, cancellationToken);
            if (document?.FileSizeBytes > settings.LargeFileSizeThresholdBytes)
                return true;
        }

        return false;
    }

    public async Task<bool> RequiresApprovalAsync(DocumentShare documentShare,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);

        // CRITICAL: If global approval is disabled, no shares require approval regardless of other settings
        if (!settings.IsGlobalApprovalEnabled)
        {
            logger.LogInformation("Global approval system is disabled - no approval required for share {ShareId}",
                documentShare.Id);
            return false;
        }

        // Global approval is enabled, check specific rules
        if (settings.ForceApprovalForAll)
        {
            logger.LogInformation("ForceApprovalForAll is enabled - approval required for share {ShareId}",
                documentShare.Id);
            return true;
        }



        if (settings.ForceApprovalForLargeFiles)
        {
            var document = await unitOfWork.Documents.GetByIdAsync(documentShare.DocumentId, cancellationToken);
            if (document?.FileSizeBytes > settings.LargeFileSizeThresholdBytes)
            {
                logger.LogInformation("ForceApprovalForLargeFiles is enabled - approval required for large file share {ShareId} (size: {FileSize} bytes)",
                    documentShare.Id, document.FileSizeBytes);
                return true;
            }
        }

        logger.LogInformation("No approval rules matched - no approval required for share {ShareId}",
            documentShare.Id);
        return false;
    }

    public async Task<ApprovalPolicy> EnsureDefaultPolicyExistsAsync(CancellationToken cancellationToken = default)
    {
        return await EnsureGlobalSystemPolicyExistsAsync(cancellationToken);
    }

    public async Task<ApprovalPolicy> EnsureGlobalSystemPolicyExistsAsync(CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var globalPolicy = await unitOfWork.ApprovalPolicies.GetGlobalSystemPolicyAsync(cancellationToken);

            if (globalPolicy == null)
            {
                var defaultPolicyId = Guid.Parse("00000000-0000-0000-0002-000000000001");
                globalPolicy = await unitOfWork.ApprovalPolicies.GetByIdAsync(defaultPolicyId, cancellationToken);

                if (globalPolicy == null)
                {
                    logger.LogInformation("Creating Global System Policy manually (seeder data not found)");

                    globalPolicy = new ApprovalPolicy
                    {
                        Id = Guid.NewGuid(),
                        Name = "Global System Policy",
                        Description =
                            "Core system policy that handles all approval requirements. This policy cannot be deleted and is automatically applied when specific policies don't match.",
                        IsActive = true,
                        Priority = 999,
                        RequireApproval = true,
                        ApprovalTimeoutHours = 72,
                        CreatedBy = null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await unitOfWork.ApprovalPolicies.AddAsync(globalPolicy, cancellationToken);

                    logger.LogInformation("Global System Policy created manually: {PolicyId}", globalPolicy.Id);
                }
                else
                {
                    logger.LogInformation("Using Global System Policy from database seeder: {PolicyId}", globalPolicy.Id);
                }
            }

            if (!globalPolicy.IsActive)
            {
                globalPolicy.IsActive = true;
                globalPolicy.UpdatedAt = DateTime.UtcNow;
                unitOfWork.ApprovalPolicies.Update(globalPolicy);

                logger.LogInformation("Global System Policy reactivated: {PolicyId}", globalPolicy.Id);
            }

            return globalPolicy;
        }, cancellationToken);
    }

    private async Task<ApprovalSettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await unitOfWork.ApprovalSettings.GetCurrentAsync(cancellationToken);

        if (settings == null)
        {
            var defaultSettingsId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            settings = await unitOfWork.ApprovalSettings.GetByIdAsync(defaultSettingsId, cancellationToken);

            if (settings == null)
            {
                return await unitOfWork.ExecuteTransactionalAsync(async () =>
                {
                    settings = new ApprovalSettings
                    {
                        Id = Guid.NewGuid(),
                        IsGlobalApprovalEnabled = false,
                        DefaultExpirationDays = 7,
                        DefaultRequiredApprovals = 1,
                        ForceApprovalForAll = false,
                        ForceApprovalForLargeFiles = true,
                        LargeFileSizeThresholdBytes = 100 * 1024 * 1024,
                        EnableEmailNotifications = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await unitOfWork.ApprovalSettings.AddAsync(settings, cancellationToken);

                    logger.LogInformation("Default approval settings created manually (seeder data not found)");
                    
                    return settings;
                }, cancellationToken);
            }
            else
            {
                logger.LogInformation("Using default approval settings from database seeder");
            }
        }

        return settings;
    }
}