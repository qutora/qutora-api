using System.Text.Json;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Approval;
using Qutora.Shared.Enums;
using Qutora.Shared.Exceptions;

namespace Qutora.Application.Services;

public class ApprovalPolicyService(
    IUnitOfWork unitOfWork,
    ILogger<ApprovalPolicyService> logger,
    UserManager<ApplicationUser> userManager)
    : IApprovalPolicyService
{
    private static string? SerializeList<T>(List<T>? list)
    {
        return list?.Count > 0 ? JsonSerializer.Serialize(list) : null;
    }

    private static List<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<T>();

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    public async Task<ApprovalPolicyDto> CreateAsync(CreateApprovalPolicyDto dto,
        CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var policy = new ApprovalPolicy
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                Priority = dto.Priority,
                ApprovalTimeoutHours = dto.ApprovalTimeoutHours,
                RequiredApprovalCount = dto.RequiredApprovalCount,
                CategoryFilters = SerializeList(dto.CategoryFilters),
                ProviderFilters = SerializeList(dto.ProviderFilters),
                UserFilters = SerializeList(dto.UserFilters),
                ApiKeyFilters = SerializeList(dto.ApiKeyFilters),
                FileSizeLimitMB = dto.FileSizeLimitMB,
                FileTypeFilters = SerializeList(dto.FileTypeFilters),
                RequireApproval = dto.RequireApproval,
                CreatedBy = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await unitOfWork.ApprovalPolicies.AddAsync(policy, cancellationToken);

            logger.LogInformation("Approval policy created: {PolicyId} - {PolicyName}", policy.Id, policy.Name);

            return policy.Adapt<ApprovalPolicyDto>();
        }, cancellationToken);
    }

    public async Task<ApprovalPolicyDto?> UpdateAsync(Guid id, UpdateApprovalPolicyDto dto,
        CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var policy = await unitOfWork.ApprovalPolicies.GetByIdAsync(id, cancellationToken);
            if (policy == null)
                throw new EntityNotFoundException($"Approval policy with ID {id} not found.");

            policy.Name = dto.Name ?? policy.Name;
            policy.Description = dto.Description ?? policy.Description;
            policy.IsActive = dto.IsActive ?? policy.IsActive;
            policy.Priority = dto.Priority ?? policy.Priority;
            policy.ApprovalTimeoutHours = dto.ApprovalTimeoutHours ?? policy.ApprovalTimeoutHours;
            policy.RequiredApprovalCount = dto.RequiredApprovalCount ?? policy.RequiredApprovalCount;
            policy.CategoryFilters =
                dto.CategoryFilters != null ? SerializeList(dto.CategoryFilters) : policy.CategoryFilters;
            policy.ProviderFilters =
                dto.ProviderFilters != null ? SerializeList(dto.ProviderFilters) : policy.ProviderFilters;
            policy.UserFilters = dto.UserFilters != null ? SerializeList(dto.UserFilters) : policy.UserFilters;
            policy.ApiKeyFilters = dto.ApiKeyFilters != null ? SerializeList(dto.ApiKeyFilters) : policy.ApiKeyFilters;
            policy.FileSizeLimitMB = dto.FileSizeLimitMB ?? policy.FileSizeLimitMB;
            policy.FileTypeFilters =
                dto.FileTypeFilters != null ? SerializeList(dto.FileTypeFilters) : policy.FileTypeFilters;
            policy.RequireApproval = dto.RequireApproval ?? policy.RequireApproval;
            policy.UpdatedAt = DateTime.UtcNow;

            unitOfWork.ApprovalPolicies.Update(policy);

            logger.LogInformation("Approval policy updated: {PolicyId}", id);

            return policy.Adapt<ApprovalPolicyDto>();
        }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var policy = await unitOfWork.ApprovalPolicies.GetByIdAsync(id, cancellationToken);
            if (policy == null)
                return false;

            var isGlobalSystemPolicy =
                await unitOfWork.ApprovalPolicies.IsGlobalSystemPolicyAsync(id, cancellationToken);
            if (isGlobalSystemPolicy)
            {
                logger.LogWarning("Attempt to delete Global System Policy {PolicyId} - this policy is protected", id);
                throw new InvalidOperationException(
                    "Global System Policy cannot be deleted. This policy is required for system operation.");
            }

            var allRequests = await unitOfWork.ShareApprovalRequests.GetAllAsync(cancellationToken);
            var hasPendingRequests =
                allRequests.Any(x => x.ApprovalPolicyId == id && x.Status == ApprovalStatus.Pending);

            if (hasPendingRequests)
            {
                logger.LogWarning("Cannot delete policy {PolicyId} - has pending approval requests", id);
                throw new InvalidOperationException(
                    "Cannot delete policy with pending approval requests. Please wait for all requests to be processed.");
            }

            unitOfWork.ApprovalPolicies.Remove(policy);

            logger.LogInformation("Approval policy deleted: {PolicyId}", id);
            return true;
        }, cancellationToken);
    }

    public async Task<ApprovalPolicyDto?> TogglePolicyStatusAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var policy = await unitOfWork.ApprovalPolicies.GetByIdAsync(id, cancellationToken);
            if (policy == null)
                return null;

            var isGlobalSystemPolicy =
                await unitOfWork.ApprovalPolicies.IsGlobalSystemPolicyAsync(id, cancellationToken);
            if (isGlobalSystemPolicy)
            {
                logger.LogWarning(
                    "Attempt to toggle Global System Policy {PolicyId} status - this policy must remain active", id);
                throw new InvalidOperationException(
                    "Global System Policy cannot be deactivated. This policy must remain active for system operation.");
            }

            policy.IsActive = !policy.IsActive;
            policy.UpdatedAt = DateTime.UtcNow;

            unitOfWork.ApprovalPolicies.Update(policy);

            logger.LogInformation("Approval policy {PolicyId} status toggled to {Status}", id, policy.IsActive);
            return policy.Adapt<ApprovalPolicyDto>();
        }, cancellationToken);
    }

    public async Task<ApprovalPolicyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await unitOfWork.ApprovalPolicies.GetByIdAsync(id, cancellationToken);
        return policy?.Adapt<ApprovalPolicyDto>();
    }

    public async Task<PagedDto<ApprovalPolicyDto>> GetPagedAsync(ApprovalPolicyQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var pagedResult = await unitOfWork.ApprovalPolicies.GetPagedAsync(query, cancellationToken);

        var dtos = pagedResult.Items.Adapt<List<ApprovalPolicyDto>>();

        return new PagedDto<ApprovalPolicyDto>
        {
            Items = dtos,
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages
        };
    }

    public async Task<ApprovalPolicy?> GetApplicablePolicyAsync(DocumentShare documentShare,
        CancellationToken cancellationToken = default)
    {
        var activePolicies = await unitOfWork.ApprovalPolicies.GetActiveAsync(cancellationToken);

        foreach (var policy in activePolicies.OrderBy(x => x.Priority))
        {
            if (policy.Name == "Global System Policy")
                continue;

            if (await EvaluatePolicyAsync(policy, null, null, documentShare, cancellationToken)) return policy;
        }

        var globalPolicy = await unitOfWork.ApprovalPolicies.GetGlobalSystemPolicyAsync(cancellationToken);
        if (globalPolicy?.IsActive == true) return globalPolicy;

        return null;
    }

    public async Task<bool> EvaluatePolicyAsync(ApprovalPolicy policy, Document? document, ApplicationUser? user,
        DocumentShare share, CancellationToken cancellationToken = default)
    {
        if (!policy.IsActive || !policy.RequireApproval)
            return false;

        document ??= await unitOfWork.Documents.GetByIdAsync(share.DocumentId, cancellationToken);
        if (document == null)
            return false;

        var categoryFilters = DeserializeList<Guid>(policy.CategoryFilters);
        if (categoryFilters.Any() &&
            (document.CategoryId == null || !categoryFilters.Contains(document.CategoryId.Value)))
            return false;

        var providerFilters = DeserializeList<Guid>(policy.ProviderFilters);
        if (providerFilters.Any() && !providerFilters.Contains(document.StorageProviderId))
            return false;

        var userFilters = DeserializeList<string>(policy.UserFilters);
        if (userFilters.Any() && !userFilters.Contains(share.CreatedBy))
            return false;

        var apiKeyFilters = DeserializeList<Guid>(policy.ApiKeyFilters);
        if (apiKeyFilters.Any() &&
            (!share.CreatedViaApiKeyId.HasValue || !apiKeyFilters.Contains(share.CreatedViaApiKeyId.Value)))
            return false;

        if (policy.FileSizeLimitMB.HasValue)
        {
            var maxSizeBytes = policy.FileSizeLimitMB.Value * 1024L * 1024L;
            if (document.FileSizeBytes > maxSizeBytes)
                return true;
        }

        var fileTypeFilters = DeserializeList<string>(policy.FileTypeFilters);
        if (fileTypeFilters.Any())
        {
            var fileExtension = Path.GetExtension(document.FileName)?.TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !fileTypeFilters.Contains(fileExtension))
                return false;
        }

        return true;
    }

    public async Task<List<string>> GetAssignedApproversAsync(ApprovalPolicy policy, DocumentShare share,
        CancellationToken cancellationToken = default)
    {
        var assignedApprovers = new List<string>();

        if (!string.IsNullOrEmpty(policy.UserFilters))
        {
            var userFilters = DeserializeList<string>(policy.UserFilters);
            if (userFilters.Any())
            {
                assignedApprovers.AddRange(userFilters);
                return assignedApprovers;
            }
        }

        var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
        if (adminUsers.Any())
        {
            assignedApprovers.AddRange(adminUsers.Select(u => u.Id));
            return assignedApprovers;
        }

        var managerUsers = await userManager.GetUsersInRoleAsync("Manager");
        if (managerUsers.Any())
        {
            assignedApprovers.AddRange(managerUsers.Select(u => u.Id));
            return assignedApprovers;
        }

        return assignedApprovers;
    }

    public async Task<ApprovalPolicy?> GetDefaultPolicyAsync(CancellationToken cancellationToken = default)
    {
        return await unitOfWork.ApprovalPolicies.GetDefaultForcedPolicyAsync(cancellationToken);
    }

    public async Task<List<ApprovalPolicyDto>> GetApplicablePoliciesAsync(DocumentShare documentShare,
        CancellationToken cancellationToken = default)
    {
        var allPolicies = await unitOfWork.ApprovalPolicies.GetActiveAsync(cancellationToken);
        var applicablePolicies = new List<ApprovalPolicy>();

        foreach (var policy in allPolicies)
            if (await EvaluatePolicyAsync(policy, null, null, documentShare, cancellationToken))
                applicablePolicies.Add(policy);

        var sortedPolicies = applicablePolicies.OrderBy(x => x.Priority).ToList();
        return sortedPolicies.Adapt<List<ApprovalPolicyDto>>();
    }

    public async Task<bool> EvaluateApprovalRequirementAsync(DocumentShare documentShare,
        CancellationToken cancellationToken = default)
    {
        var applicablePolicies = await GetApplicablePoliciesAsync(documentShare, cancellationToken);
        return applicablePolicies.Any();
    }
}