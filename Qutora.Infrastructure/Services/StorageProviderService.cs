using System.Text.Json;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Application.Interfaces.Storage;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Application.Security;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Infrastructure.Storage.Models;
using Qutora.Shared.DTOs;

namespace Qutora.Infrastructure.Services;

public class StorageProviderService(
    IStorageProviderRepository storageProviderRepository,
    ISensitiveDataProtector dataProtector,
    ILogger<StorageProviderService> logger,
    IStorageManager storageManager,
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    IServiceProvider serviceProvider,
    ICurrentUserService currentUserService)
    : IStorageProviderService
{
    /// <summary>
    /// Executes operations with error management helper method
    /// </summary>
    private async Task<T> ExecuteWithLoggingAsync<T>(Func<Task<T>> action, string errorMessage)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, errorMessage);
            throw;
        }
    }

    /// <summary>
    /// Error management helper method for synchronous operations
    /// </summary>
    private T ExecuteWithLogging<T>(Func<T> action, string errorMessage)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, errorMessage);
            throw;
        }
    }

    /// <summary>
    /// Determines sensitive config keys based on provider type
    /// </summary>
    private string[] GetSensitiveConfigKeys(string providerType)
    {
        return providerType.ToLowerInvariant() switch
        {
            "minio" => ["accessKey", "secretKey"],
            "ftp" => ["password"],
            "sftp" => ["password", "privateKey", "privateKeyPassphrase"],
            _ => []
        };
    }

    public async Task<IEnumerable<StorageProviderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(
            async () =>
            {
                var entities = await storageProviderRepository.GetAllAsync(cancellationToken);
                return entities.Adapt<List<StorageProviderDto>>();
            },
            "Error occurred while getting all storage providers");
    }

    public async Task<IEnumerable<StorageProviderDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(
            async () =>
            {
                var entities = await storageProviderRepository.GetAllActiveAsync(cancellationToken);
                return entities.Adapt<List<StorageProviderDto>>();
            },
            "Error occurred while getting all active storage providers");
    }

    public async Task<StorageProviderDto?> GetDefaultProviderAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(
            async () =>
            {
                var entity = await storageProviderRepository.GetDefaultProviderAsync(cancellationToken);
                if (entity == null) return null;

                return entity.Adapt<StorageProviderDto>();
            },
            "Error occurred while getting default storage provider");
    }

    public async Task<StorageProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(
            async () =>
            {
                var entity = await storageProviderRepository.GetByIdAsync(id, cancellationToken);
                if (entity == null) return null;

                return entity.Adapt<StorageProviderDto>();
            },
            $"Error occurred while getting storage provider by id: {id}");
    }

    public async Task<IEnumerable<string>> GetAvailableProviderNamesAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(
            async () =>
            {
                var providers = await storageProviderRepository.GetAllActiveAsync(cancellationToken);
                return providers.Select(p => p.Id.ToString());
            },
            "Error occurred while getting available provider IDs");
    }

    public IEnumerable<string> GetAvailableProviderTypes()
    {
        return ExecuteWithLogging(() =>
        {
            var registry = serviceProvider.GetRequiredService<IStorageProviderTypeRegistry>();
            return registry.GetAvailableProviderTypes();
        }, "Error occurred while getting available provider types from registry");
    }

    public string GetConfigurationSchema(string providerType)
    {
        return ExecuteWithLogging(() =>
            {
                var schema = ProviderConfigFactory.GetSchema(providerType);
                if (schema == null || schema.Count == 0)
                    throw new ArgumentException($"Unsupported provider type: {providerType}");

                return JsonSerializer.Serialize(schema);
            }, $"Error occurred while getting provider config schema for type: {providerType}");
    }

    public async Task<StorageProviderDto> CreateAsync(StorageProviderCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(async () =>
        {
            if (dto.ConfigurationValues != null)
            {
                var configJson = JsonSerializer.Serialize(dto.ConfigurationValues);

                dto.ConfigJson = dataProtector.ProtectSensitiveConfigJson(configJson, dto.ProviderType);
            }

            // CONNECTION TEST BEFORE CREATING PROVIDER
            var testDto = new StorageProviderTestDto
            {
                ProviderType = dto.ProviderType,
                ConfigJson = dto.ConfigJson
            };
            
            var connectionTest = await TestConnectionAsync(testDto, cancellationToken);
            if (!connectionTest.success)
            {
                throw new InvalidOperationException("Connection test failed, provider could not be added.");
            }

            var provider = new StorageProvider
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                ProviderType = dto.ProviderType,
                ConfigJson = dto.ConfigJson,
                IsDefault = dto.IsDefault,
                IsActive = true,
                MaxFileSize = dto.MaxFileSize,
                CreatedAt = DateTime.UtcNow,
                Description = dto.Description
            };

            var providers = await storageProviderRepository.GetAllAsync(cancellationToken);
            if (!providers.Any(p => p.IsDefault)) provider.IsDefault = true;

            var result = await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await storageProviderRepository.AddAsync(provider, cancellationToken);
                return provider.Adapt<StorageProviderDto>();
            }, cancellationToken);

            await storageManager.ReloadProvidersAsync();

            if (ShouldCreateBucketForProvider(provider.ProviderType) && dto.ConfigurationValues != null)
            {
                string? bucketName = null;

                if (provider.ProviderType.ToLowerInvariant() == "filesystem" ||
                    provider.ProviderType.ToLowerInvariant() == "ftp" ||
                    provider.ProviderType.ToLowerInvariant() == "sftp")
                {
                    string? rootDirValue = null;

                    if (provider.ProviderType.ToLowerInvariant() == "filesystem")
                    {
                        if (dto.ConfigurationValues.TryGetValue("rootPath", out var rootPathObj))
                            rootDirValue = rootPathObj?.ToString()?.Trim();
                        else if (dto.ConfigurationValues.TryGetValue("basePath", out var basePathObj))
                            rootDirValue = basePathObj?.ToString()?.Trim();
                        else if (dto.ConfigurationValues.TryGetValue("rootDirectory", out var rootDirObj))
                            rootDirValue = rootDirObj?.ToString()?.Trim();
                    }
                    else
                    {
                        if (dto.ConfigurationValues.TryGetValue("rootDirectory", out var rootDirObj))
                            rootDirValue = rootDirObj?.ToString()?.Trim();
                    }

                    if (!string.IsNullOrEmpty(rootDirValue) && rootDirValue != "/" && rootDirValue != "\\" &&
                        rootDirValue != ".")
                    {
                        bucketName = Path.GetFileName(rootDirValue.Trim('/', '\\')) ?? "default";
                        if (string.IsNullOrEmpty(bucketName)) bucketName = "root";
                    }
                    else
                    {
                        bucketName = "root";
                    }
                }
                else
                {
                    if (dto.ConfigurationValues.TryGetValue("bucketName", out var bucketNameObj))
                        bucketName = bucketNameObj?.ToString();
                }

                if (!string.IsNullOrEmpty(bucketName))
                    try
                    {
                        await CreateProviderBucketAsync(provider.Id, bucketName, provider.Name, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "Bucket creation failed but provider was created: {ProviderId}, {BucketName}, {ProviderType}",
                            provider.Id, bucketName, provider.ProviderType);
                    }
            }

            return result;
        }, "Error occurred while creating storage provider");
    }

    public async Task<bool> UpdateAsync(Guid id, StorageProviderUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(async () =>
            {
                var provider = await storageProviderRepository.GetByIdAsync(id, cancellationToken);
                if (provider == null) return false;

                if (dto.ConfigurationValues != null)
                {
                    var configJson = JsonSerializer.Serialize(dto.ConfigurationValues);

                    dto.ConfigJson = dataProtector.ProtectSensitiveConfigJson(configJson, provider.ProviderType);
                }

                // CONNECTION TEST BEFORE UPDATING PROVIDER
                var testDto = new StorageProviderTestDto
                {
                    Id = id,
                    ProviderType = provider.ProviderType,
                    ConfigJson = dto.ConfigJson
                };
                
                var connectionTest = await TestConnectionAsync(testDto, cancellationToken);
                if (!connectionTest.success)
                {
                    throw new InvalidOperationException("Connection test failed, provider could not be updated.");
                }

                provider.Name = dto.Name;
                provider.ConfigJson = dto.ConfigJson;
                provider.IsDefault = dto.IsDefault;
                provider.MaxFileSize = dto.MaxFileSize;
                provider.UpdatedAt = DateTime.UtcNow;
                provider.Description = dto.Description;

                return await unitOfWork.ExecuteTransactionalAsync(async () =>
                {
                    if (provider.IsDefault)
                    {
                        var setDefaultResult =
                            await storageProviderRepository.SetAsDefaultAsync(id, cancellationToken);
                        if (!setDefaultResult)
                        {
                            logger.LogWarning("Failed to set provider {ProviderId} as default during update", id);
                            return false;
                        }
                    }

                    await storageProviderRepository.UpdateAsync(provider, cancellationToken);

                    await storageManager.ReloadProvidersAsync();

                    return true;
                }, cancellationToken);
            }, $"Error occurred while updating storage provider: {id}");
    }

    public async Task<bool> ToggleStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(async () =>
            {
                var provider = await storageProviderRepository.GetByIdAsync(id, cancellationToken);
                if (provider == null) return false;

                provider.IsActive = isActive;
                provider.UpdatedAt = DateTime.UtcNow;

                return await unitOfWork.ExecuteTransactionalAsync(async () =>
                {
                    await storageProviderRepository.UpdateAsync(provider, cancellationToken);

                    await storageManager.RemoveProviderFromCacheAsync(id.ToString());
                    await storageManager.ReloadProvidersAsync();

                    return true;
                }, cancellationToken);
            }, $"Error occurred while toggling status for storage provider: {id}");
    }

    public async Task<bool> SetAsDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(async () =>
            {
                var provider = await storageProviderRepository.GetByIdAsync(id, cancellationToken);
                if (provider == null) return false;

                return await unitOfWork.ExecuteTransactionalAsync(async () =>
                {
                    var setDefaultResult = await storageProviderRepository.SetAsDefaultAsync(id, cancellationToken);
                    if (!setDefaultResult)
                    {
                        logger.LogError("Failed to set provider {ProviderId} as default", id);
                        return false;
                    }

                    await storageManager.ReloadProvidersAsync();

                    return true;
                }, cancellationToken);
            }, $"Error occurred while setting default storage provider: {id}");
    }

    public async Task<(bool success, string message)> TestConnectionAsync(StorageProviderTestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!string.IsNullOrEmpty(dto.ConfigJson))
                dto.ConfigJson = dataProtector.UnprotectSensitiveConfigJson(dto.ConfigJson, dto.ProviderType);

            return await storageManager.TestProviderConnectionAsync(dto.ProviderType, dto.ConfigJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Provider connection test failed");
            return (false, $"Connection test failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a storage provider
    /// </summary>
    /// <param name="id">ID of the storage provider to delete</param>
    /// <returns>True if operation is successful, false otherwise</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(async () =>
            {
                var provider = await storageProviderRepository.GetByIdAsync(id, cancellationToken);
                if (provider == null) return false;

                if (provider.IsDefault)
                    throw new InvalidOperationException("Default storage provider cannot be deleted.");

                await CheckProviderDependenciesAsync(id, cancellationToken);

                return await unitOfWork.ExecuteTransactionalAsync(async () =>
                {
                    storageProviderRepository.Remove(provider);

                    await storageManager.ReloadProvidersAsync();

                    return true;
                }, cancellationToken);
            }, $"Error occurred while deleting storage provider: {id}");
    }

    /// <summary>
    /// Is bucket creation required for this provider type?
    /// </summary>
    private static bool ShouldCreateBucketForProvider(string providerType)
    {
        return providerType.ToLowerInvariant() switch
        {
            "minio" => true,
            "filesystem" => true,
            "ftp" => true,
            "sftp" => true,
            _ => false
        };
    }

    /// <summary>
    /// Creates bucket for provider
    /// </summary>
    private async Task CreateProviderBucketAsync(Guid providerId, string bucketName, string providerName,
        CancellationToken cancellationToken = default)
    {
        var existing =
            await unitOfWork.StorageBuckets.GetBucketByPathAndProviderAsync(bucketName, providerId, cancellationToken);
        if (existing != null)
        {
            logger.LogInformation("Bucket already exists in database: {BucketName}, {ProviderId}", bucketName,
                providerId);
            return;
        }

        var providerInstance = await storageManager.GetProviderAsync(providerId.ToString());
        var bucketExists = await providerInstance.BucketExistsAsync(bucketName);

        if (!bucketExists)
        {
            var bucketCreated = await providerInstance.CreateBucketAsync(bucketName);
            if (!bucketCreated)
                throw new InvalidOperationException($"Bucket could not be created on provider: {bucketName}");

            logger.LogInformation("Created bucket on provider: {BucketName}, {ProviderId}", bucketName, providerId);
        }

        var bucket = new StorageBucket
        {
            Id = Guid.NewGuid(),
                                Path = bucketName,
            ProviderId = providerId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            IsPublic = false,
            IsActive = true,
            IsDefault = true,
            Description = $"Default bucket for {providerName}"
        };

        await unitOfWork.StorageBuckets.AddAsync(bucket, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Saved bucket to database: {BucketName}, {ProviderId}", bucketName, providerId);
    }

    /// <summary>
    /// Checks provider dependencies and throws appropriate error message if any exist
    /// </summary>
    private async Task CheckProviderDependenciesAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var buckets = await unitOfWork.StorageBuckets.GetByProviderIdAsync(providerId, cancellationToken);
        var bucketCount = buckets?.Count() ?? 0;

        var documents =
            await unitOfWork.Documents.GetByProviderIdAsync(providerId, cancellationToken: cancellationToken);
        var documentCount = documents?.Count() ?? 0;

        if (bucketCount > 0 || documentCount > 0)
        {
            var errorMessage = "This storage provider cannot be deleted because there are still records in use:\n";

            if (bucketCount > 0) errorMessage += $"• {bucketCount} storage buckets\n";

            if (documentCount > 0) errorMessage += $"• {documentCount} documents\n";

            errorMessage += "\nFirst delete these records or move them to another provider.";

            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Dynamically sets the property value of an object
    /// </summary>
    private void SetPropertyValue(object target, string propertyName, object? value)
    {
        if (target == null || string.IsNullOrEmpty(propertyName)) return;

        var prop = target.GetType().GetProperty(propertyName);
        if (prop != null && prop.CanWrite)
            try
            {
                prop.SetValue(target, value);
            }
            catch
            {
            }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<StorageProviderDto>> GetUserAccessibleProvidersAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithLoggingAsync(async () =>
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException($"User not found: {userId}");

            // Users with admin permission can see all active providers
            var authService = serviceProvider.GetRequiredService<IAuthorizationService>();
            var authResult = await authService.AuthorizeAsync(
                currentUserService.User, 
                "StorageProvider.Admin");
            
            if (authResult.Succeeded)
            {
                return await GetAllActiveAsync(cancellationToken);
            }

            // Normal users can only see providers they have bucket permission for
            var buckets = await unitOfWork.StorageBuckets.GetUserAccessibleBucketsAsync(userId);

            var providerIds = buckets.Select(b => b.ProviderId).Distinct().ToList();

            var providers = new List<StorageProviderDto>();
            foreach (var providerId in providerIds)
            {
                var provider = await GetByIdAsync(providerId, cancellationToken);
                if (provider != null && provider.IsActive) // Only active providers
                {
                    providers.Add(provider);
                }
            }

            return providers;
        }, "Error occurred while getting user accessible providers");
    }
}
