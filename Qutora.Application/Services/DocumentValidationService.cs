using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Shared.DTOs.DocumentOrchestration;

namespace Qutora.Application.Services;

/// <summary>
/// Focused service for document validation operations
/// Extracted from DocumentService to follow SRP
/// </summary>
public class DocumentValidationService(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IMetadataService metadataService,
    ILogger<DocumentValidationService> logger)
    : IDocumentValidationService
{
    /// <summary>
    /// Validates document creation inputs and user permissions
    /// </summary>
    public async Task ValidateDocumentCreationAsync(
        IFormFile file, 
        string name, 
        Guid? storageProviderId, 
        CancellationToken cancellationToken)
    {
        // User authentication check
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Unauthorized document creation attempt");
            throw new UnauthorizedAccessException("User authentication required.");
        }

        // Input validation
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogWarning("Document creation attempted with empty name by user {UserId}", userId);
            throw new ArgumentException("Document name cannot be empty.", nameof(name));
        }

        if (file.Length == 0)
        {
            logger.LogWarning("Document creation attempted with empty file by user {UserId}", userId);
            throw new ArgumentException("File cannot be empty.", nameof(file));
        }

        // Storage provider validation
        if (storageProviderId.HasValue)
        {
            await ValidateStorageProviderAsync(storageProviderId.Value, cancellationToken);
        }

        // File size validation (example: 100MB limit)
        const long maxFileSize = 100 * 1024 * 1024; // 100MB
        if (file.Length > maxFileSize)
        {
            logger.LogWarning("File too large: {FileSize} bytes for user {UserId}", file.Length, userId);
            throw new ArgumentException($"File size cannot exceed {maxFileSize / (1024 * 1024)}MB.");
        }

        // Content type validation (basic check)
        if (string.IsNullOrEmpty(file.ContentType))
        {
            logger.LogWarning("File with no content type uploaded by user {UserId}", userId);
            throw new ArgumentException("File content type is required.");
        }

        logger.LogDebug("Document creation validation passed for user {UserId}, file: {FileName}", 
            userId, file.FileName);
    }

    /// <summary>
    /// Validates storage provider exists and is active
    /// </summary>
    private async Task ValidateStorageProviderAsync(Guid storageProviderId, CancellationToken cancellationToken)
    {
        var provider = await unitOfWork.StorageProviders.GetByIdAsync(storageProviderId, cancellationToken);
        
        if (provider == null)
        {
            logger.LogWarning("Storage provider not found: {ProviderId}", storageProviderId);
            throw new KeyNotFoundException($"Storage provider not found: {storageProviderId}");
        }

        if (!provider.IsActive)
        {
            logger.LogWarning("Inactive storage provider used: {ProviderId} - {ProviderName}", 
                storageProviderId, provider.Name);
            throw new InvalidOperationException($"Storage provider is not active: {provider.Name}");
        }

        logger.LogDebug("Storage provider validation passed: {ProviderId} - {ProviderName}", 
            storageProviderId, provider.Name);
    }

    public async Task<ValidationResult> ValidateFileAsync(IFormFile file, Guid? providerId = null, string? metadataSchemaId = null)
    {
        try
        {
            // Basic file validation
            if (file == null || file.Length == 0)
            {
                return ValidationResult.Failure("No file uploaded or file is empty.");
            }

            // File size validation
            if (providerId.HasValue)
            {
                var sizeValidation = await ValidateFileSizeAsync(file, providerId.Value);
                if (!sizeValidation.IsValid)
                    return sizeValidation;
            }

            // File type validation
            if (!string.IsNullOrEmpty(metadataSchemaId))
            {
                var typeValidation = await ValidateFileTypeAsync(file, metadataSchemaId);
                if (!typeValidation.IsValid)
                    return typeValidation;
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating file");
            return ValidationResult.Failure("File validation failed due to an internal error.");
        }
    }

    public async Task<ValidationResult> ValidateFileSizeAsync(IFormFile file, Guid providerId)
    {
        try
        {
            var provider = await unitOfWork.StorageProviders.GetByIdAsync(providerId);
            if (provider != null && provider.MaxFileSize > 0 && file.Length > provider.MaxFileSize)
            {
                var maxSizeMB = provider.MaxFileSize / (1024.0 * 1024.0);
                var fileSizeMB = file.Length / (1024.0 * 1024.0);
                
                var errorDetails = new Dictionary<string, object>
                {
                    ["maxSizeBytes"] = provider.MaxFileSize,
                    ["fileSizeBytes"] = file.Length,
                    ["providerName"] = provider.Name
                };

                return ValidationResult.Failure(
                    $"File size ({fileSizeMB:F2} MB) exceeds the maximum allowed size ({maxSizeMB:F2} MB) for provider '{provider.Name}'.",
                    errorDetails);
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating file size for provider {ProviderId}", providerId);
            return ValidationResult.Failure("File size validation failed due to an internal error.");
        }
    }

    public async Task<ValidationResult> ValidateFileTypeAsync(IFormFile file, string metadataSchemaId)
    {
        try
        {
            if (!Guid.TryParse(metadataSchemaId, out var schemaId))
            {
                return ValidationResult.Success(); // Skip validation if schema ID is invalid
            }

            var schema = await unitOfWork.MetadataSchemas.GetByIdAsync(schemaId);
            if (schema?.FileTypes == null || schema.FileTypes.Length == 0)
            {
                return ValidationResult.Success(); // No file type restrictions
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isAllowed = false;

            foreach (var allowedType in schema.FileTypes.Split(','))
            {
                var normalizedType = allowedType.Trim().ToLowerInvariant();

                // MIME type pattern check (e.g., "image/*")
                if (normalizedType.EndsWith("/*"))
                {
                    var baseType = normalizedType.Replace("/*", "/");
                    if (file.ContentType.StartsWith(baseType))
                    {
                        isAllowed = true;
                        break;
                    }
                }
                // Exact MIME type check
                else if (normalizedType == file.ContentType.ToLowerInvariant())
                {
                    isAllowed = true;
                    break;
                }
                // File extension check (e.g., ".pdf", ".docx")
                else if (normalizedType.StartsWith(".") && normalizedType == fileExtension)
                {
                    isAllowed = true;
                    break;
                }
            }

            if (!isAllowed)
            {
                var errorDetails = new Dictionary<string, object>
                {
                    ["allowedTypes"] = schema.FileTypes,
                    ["schemaName"] = schema.Name,
                    ["fileContentType"] = file.ContentType,
                    ["fileExtension"] = fileExtension
                };

                return ValidationResult.Failure(
                    $"File type '{file.ContentType}' (extension: '{fileExtension}') is not allowed by the selected metadata schema. Allowed types: {string.Join(", ", schema.FileTypes)}",
                    errorDetails);
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating file type for schema {SchemaId}", metadataSchemaId);
            return ValidationResult.Failure("File type validation failed due to an internal error.");
        }
    }

    public async Task<ValidationResult> ValidateMetadataAsync(string? metadataJson, string? schemaName)
    {
        try
        {
            if (string.IsNullOrEmpty(metadataJson) || string.IsNullOrEmpty(schemaName))
            {
                return ValidationResult.Success(); // No metadata to validate
            }

            var metadataValues = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
            if (metadataValues == null || metadataValues.Count == 0)
            {
                return ValidationResult.Success(); // No metadata values
            }

            var validationErrors = await metadataService.ValidateMetadataAsync(schemaName, metadataValues);
            if (validationErrors.Count > 0)
            {
                var errorMessages = string.Join("; ", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                return ValidationResult.Failure($"Metadata validation failed: {errorMessages}");
            }

            return ValidationResult.Success();
        }
        catch (JsonException)
        {
            return ValidationResult.Failure("Invalid metadata JSON format");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating metadata");
            return ValidationResult.Failure("Metadata validation failed due to an internal error.");
        }
    }
} 