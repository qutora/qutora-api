using Mapster;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Authentication;
using Qutora.Application.Models.ApiKeys;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.DTOs.Approval;
using Qutora.Shared.DTOs.Email;

namespace Qutora.Application.Mappings;

/// <summary>
/// Mapster konfigürasyon sınıfı
/// </summary>
public static class MappingConfig
{
    /// <summary>
    /// Tüm mapping konfigürasyonlarını yapılandırır
    /// </summary>
    public static TypeAdapterConfig RegisterMappings()
    {
        var config = new TypeAdapterConfig();

        config.NewConfig<ApplicationUser, UserDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.LastLogin, src => src.LastLogin);

        config.NewConfig<Document, DocumentDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.ContentType, src => src.ContentType)
            .Map(dest => dest.FileSize, src => src.FileSize)
            .Map(dest => dest.StoragePath, src => src.StoragePath)
            .Map(dest => dest.Hash, src => src.Hash)
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.CategoryName, src => src.Category != null ? src.Category.Name : null)
            .Map(dest => dest.StorageProviderId, src => src.StorageProviderId)
            .Map(dest => dest.StorageProviderName, src => src.StorageProvider != null ? src.StorageProvider.Name : null)
            .Map(dest => dest.BucketId, src => src.BucketId)
            .Map(dest => dest.BucketPath, src => src.Bucket != null ? src.Bucket.Path : null)
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.CreatedByName, src => src.CreatedByUser != null ? $"{src.CreatedByUser.FirstName} {src.CreatedByUser.LastName}" : null)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.LastAccessedAt, src => src.LastAccessedAt);
            // Approval fields removed from Document entity

        config.NewConfig<Category, CategoryDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.ParentCategoryId, src => src.ParentCategoryId)
            .Map(dest => dest.ParentCategoryName,
                src => src.ParentCategory != null ? src.ParentCategory.Name : null)
            .Map(dest => dest.DocumentCount, src => src.Documents != null ? src.Documents.Count(d => !d.IsDeleted) : 0)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.AllowDirectAccess, src => src.AllowDirectAccess);

        config.NewConfig<CreateCategoryDto, Category>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.ParentCategoryId, src => src.ParentCategoryId)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.IsDeleted, src => false);

        config.NewConfig<UpdateCategoryDto, Category>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.ParentCategoryId, src => src.ParentCategoryId)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);

        config.NewConfig<CreateDocumentDto, Document>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.StorageProviderId, src => src.StorageProviderId)
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.IsDeleted, src => false);

        config.NewConfig<UpdateDocumentDto, Document>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);

        config.NewConfig<MetadataSchema, MetadataSchemaDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Version, src => src.Version)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.FileTypes, src => src.FileTypeArray)
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.Fields, src => src.Fields.Select(f => new MetadataSchemaFieldDto
            {
                Name = f.Name,
                DisplayName = f.DisplayName,
                Description = f.Description,
                Type = f.Type,
                IsRequired = f.IsRequired,
                DefaultValue = f.DefaultValue,
                MinValue = f.MinValue,
                MaxValue = f.MaxValue,
                MinLength = f.MinLength,
                MaxLength = f.MaxLength,
                ValidationRegex = f.ValidationRegex,
                Order = f.Order,
                OptionItems = f.OptionItems.OrderBy(o => o.Order).Select(o => new MetadataSchemaFieldOptionDto
                {
                    Label = o.Label,
                    Value = o.Value,
                    IsDefault = o.IsDefault,
                    Order = o.Order
                }).ToList()
            }).OrderBy(f => f.Order).ToList())
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        config.NewConfig<Metadata, MetadataDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.DocumentId, src => src.DocumentId)
            .Map(dest => dest.DocumentName, src => src.Document != null ? src.Document.Name : null)
            .Map(dest => dest.SchemaName, src => src.SchemaName)
            .Map(dest => dest.SchemaVersion, src => src.SchemaVersion)
            .Map(dest => dest.Values, src => src.Values)
            .Map(dest => dest.Tags, src => src.TagArray)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        config.NewConfig<ApiKey, ApiKeyResponseDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Map(dest => dest.LastUsedAt, src => src.LastUsedAt)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.Permission, src => src.Permissions.ToString())
            .Map(dest => dest.ProviderCount, src => src.AllowedProviderIds.Count);

        config.NewConfig<StorageProvider, StorageProviderDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.ProviderType, src => src.ProviderType)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.MaxFileSize, src => src.MaxFileSize)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        config.NewConfig<BucketPermission, BucketPermissionDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.BucketId, src => src.BucketId)
            .Map(dest => dest.SubjectId, src => src.SubjectId)
            .Map(dest => dest.SubjectType, src => src.SubjectType)
            .Map(dest => dest.Permission, src => src.Permission)
            .Map(dest => dest.GrantedByName, src => src.CreatedBy)
            .Map(dest => dest.GrantedAt, src => src.CreatedAt);

        config.NewConfig<ApprovalSettings, ApprovalSettingsDto>()
            .Map(dest => dest.IsGlobalApprovalEnabled, src => src.IsGlobalApprovalEnabled)
            .Map(dest => dest.GlobalApprovalEnabledAt, src => src.GlobalApprovalEnabledAt)
            .Map(dest => dest.GlobalApprovalEnabledByUserId, src => src.GlobalApprovalEnabledByUserId)
            .Map(dest => dest.GlobalApprovalReason, src => src.GlobalApprovalReason)
            .Map(dest => dest.ForceApprovalForAll, src => src.ForceApprovalForAll)
            .Map(dest => dest.DefaultExpirationDays, src => src.DefaultExpirationDays)
            .Map(dest => dest.DefaultRequiredApprovals, src => src.DefaultRequiredApprovals)
            .Map(dest => dest.ForceApprovalForLargeFiles, src => src.ForceApprovalForLargeFiles)
            .Map(dest => dest.LargeFileSizeThresholdBytes, src => src.LargeFileSizeThresholdBytes)
            .Map(dest => dest.EnableEmailNotifications, src => src.EnableEmailNotifications)
            .Map(dest => dest.UpdatedByUserName, src => "System");

        config.NewConfig<ApprovalPolicy, ApprovalPolicyDto>();

        config.NewConfig<CreateApprovalPolicyDto, ApprovalPolicy>()
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);

        config.NewConfig<ShareApprovalRequest, ShareApprovalRequestDto>()
            .Map(dest => dest.RequesterName,
                src => src.RequestedByUser != null
                    ? $"{src.RequestedByUser.FirstName} {src.RequestedByUser.LastName}"
                    : null)
            .Map(dest => dest.RequestedByUserName,
                src => src.RequestedByUser != null ? src.RequestedByUser.UserName : null)
            .Map(dest => dest.DocumentName,
                src => src.DocumentShare != null && src.DocumentShare.Document != null
                    ? src.DocumentShare.Document.Name
                    : null)
            .Map(dest => dest.ShareCode, src => src.DocumentShare != null ? src.DocumentShare.ShareCode : null)
            .Map(dest => dest.PolicyName, src => src.ApprovalPolicy != null ? src.ApprovalPolicy.Name : null)
            .Map(dest => dest.AssignedApprovers, src => string.IsNullOrEmpty(src.AssignedApprovers)
                ? new List<string>()
                : src.AssignedApprovers.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

        config.NewConfig<ApprovalDecision, ApprovalDecisionDto>()
            .Map(dest => dest.ApproverName,
                src => src.ApproverUser != null
                    ? $"{src.ApproverUser.FirstName} {src.ApproverUser.LastName}"
                    : null)
            .Map(dest => dest.ApproverUserName,
                src => src.ApproverUser != null ? src.ApproverUser.UserName : null);

        config.NewConfig<ApprovalHistory, ApprovalHistoryDto>()
            .Map(dest => dest.ActionByUserName,
                src => src.ActionByUser != null
                    ? $"{src.ActionByUser.FirstName} {src.ActionByUser.LastName}"
                    : null);

        // Email Settings mappings
        config.NewConfig<EmailSettings, EmailSettingsDto>();

        config.NewConfig<UpdateEmailSettingsDto, EmailSettings>()
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);

        // Email Template mappings
        config.NewConfig<EmailTemplate, EmailTemplateDto>();

        config.NewConfig<CreateEmailTemplateDto, EmailTemplate>()
            .Map(dest => dest.Id, src => Guid.NewGuid())
            .Map(dest => dest.CreatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow)
            .Map(dest => dest.IsSystem, src => false);

        config.NewConfig<UpdateEmailTemplateDto, EmailTemplate>()
            .Map(dest => dest.UpdatedAt, src => DateTime.UtcNow);

        // AuditLog mapping
        config.NewConfig<AuditLog, AuditLogDto>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.UserId, src => src.UserId)
            .Map(dest => dest.UserName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null)
            .Map(dest => dest.Timestamp, src => src.Timestamp)
            .Map(dest => dest.EventType, src => src.EventType)
            .Map(dest => dest.EntityType, src => src.EntityType)
            .Map(dest => dest.EntityId, src => src.EntityId)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.Data, src => src.Data)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        return config;
    }
}