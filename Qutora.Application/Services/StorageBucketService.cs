using System.Text.RegularExpressions;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.Storage;
using Qutora.Infrastructure.Interfaces.UnitOfWork;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Services;

/// <summary>
/// Service class that manages bucket/folder operations
/// </summary>
public class StorageBucketService(
    IStorageManager storageManager,
    ILogger<StorageBucketService> logger,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IBucketPermissionManager permissionManager,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IServiceProvider serviceProvider,
    ICurrentUserService currentUserService)
    : IStorageBucketService
{
    private static readonly Dictionary<string, BucketProviderConfig> _providerConfigs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["minio"] = new BucketProviderConfig
            {
                MinBucketNameLength = 3,
                MaxBucketNameLength = 63,
                AllowedCharactersPattern = @"^[a-z0-9][a-z0-9\-\.]{1,61}[a-z0-9]$",
                AllowNestedBuckets = false,
                RequiresPermissionCheck = true,
                AllowForceDelete = true,
                ValidateBucketName = name =>
                {
                    if (name.Length < 3 || name.Length > 63)
                        return (false, "Bucket name must be 3-63 characters long.");

                    if (!Regex.IsMatch(name, @"^[a-z0-9][a-z0-9\-\.]{1,61}[a-z0-9]$"))
                        return (false,
                            "Bucket name can only contain lowercase letters, numbers, period (.) and hyphen (-) and must start and end with a letter or number.");

                    if (name.StartsWith("xn--") || name.StartsWith("sthree-"))
                        return (false, "Bucket name cannot start with 'xn--' or 'sthree-'.");

                    if (Regex.IsMatch(name, @"^(\d{1,3}\.){3}\d{1,3}$"))
                        return (false, "Bucket name cannot be in IP address format.");

                    return (true, "");
                }
            },
            ["s3"] = new BucketProviderConfig
            {
                MinBucketNameLength = 3,
                MaxBucketNameLength = 63,
                AllowedCharactersPattern = @"^[a-z0-9][a-z0-9\-\.]{1,61}[a-z0-9]$",
                AllowNestedBuckets = false,
                RequiresPermissionCheck = true,
                AllowForceDelete = true,
                ValidateBucketName = name =>
                {
                    if (name.Length < 3 || name.Length > 63)
                        return (false, "Bucket name must be 3-63 characters long.");

                    if (!Regex.IsMatch(name, @"^[a-z0-9][a-z0-9\-\.]{1,61}[a-z0-9]$"))
                        return (false,
                            "Bucket name can only contain lowercase letters, numbers, period (.) and hyphen (-) and must start and end with a letter or number.");

                    if (name.StartsWith("xn--") || name.StartsWith("sthree-"))
                        return (false, "Bucket name cannot start with 'xn--' or 'sthree-'.");

                    if (Regex.IsMatch(name, @"^(\d{1,3}\.){3}\d{1,3}$"))
                        return (false, "Bucket name cannot be in IP address format.");

                    return (true, "");
                }
            },
            ["filesystem"] = new BucketProviderConfig
            {
                MinBucketNameLength = 1,
                MaxBucketNameLength = 255,
                AllowedCharactersPattern = OperatingSystem.IsWindows() ? @"^[^<>:""/\\|?*]+$" : @"^[^/]+$",
                AllowNestedBuckets = true,
                RequiresPermissionCheck = false,
                AllowForceDelete = true,
                ValidateBucketName = name =>
                {
                    if (OperatingSystem.IsWindows())
                    {
                        string[] invalidChars = ["<", ">", ":", "\"", "/", "\\", "|", "?", "*"];
                        if (invalidChars.Any(c => name.Contains(c)))
                            return (false, "Folder name cannot contain the following characters: < > : \" / \\ | ? *");

                        string[] reservedNames =
                        [
                            "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4",
                            "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2",
                            "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
                        ];
                        if (reservedNames.Contains(name.ToUpper()))
                            return (false, $"'{name}' is a reserved file name in Windows.");

                        if (name.EndsWith(".") || name.EndsWith(" "))
                            return (false, "Folder name cannot end with period (.) or space.");
                    }
                    else
                    {
                        if (name.Contains('/'))
                            return (false, "Folder name cannot contain '/' character.");

                        if (name.StartsWith("."))
                            return (true,
                                "File name starts with '.' - this is considered a hidden file in Linux/Unix systems");
                    }

                    if (string.IsNullOrWhiteSpace(name))
                        return (false, "Folder name cannot be empty.");

                    return (true, "");
                }
            },
            ["ftp"] = new BucketProviderConfig
            {
                MinBucketNameLength = 1,
                MaxBucketNameLength = 255,
                AllowedCharactersPattern = @"^[^<>:""/\\|?*]+$",
                AllowNestedBuckets = true,
                RequiresPermissionCheck = true,
                AllowForceDelete = true,
                ValidateBucketName = name =>
                {
                    string[] invalidChars = ["<", ">", ":", "\"", "|", "?", "*"];
                    if (invalidChars.Any(c => name.Contains(c)))
                        return (false, "Folder name cannot contain the following characters: < > : \" | ? *");

                    return (true, "");
                }
            },
            ["sftp"] = new BucketProviderConfig
            {
                MinBucketNameLength = 1,
                MaxBucketNameLength = 255,
                AllowedCharactersPattern = @"^[^<>:""/\\|?*]+$",
                AllowNestedBuckets = true,
                RequiresPermissionCheck = true,
                AllowForceDelete = true,
                ValidateBucketName = name =>
                {
                    string[] invalidChars = ["<", ">", ":", "\"", "|", "?", "*"];
                    if (invalidChars.Any(c => name.Contains(c)))
                        return (false, "Folder name cannot contain the following characters: < > : \" | ? *");

                    return (true, "");
                }
            }
        };

    /// <inheritdoc/>
    public async Task<IEnumerable<BucketInfoDto>> ListProviderBucketsAsync(string providerId)
    {
        try
        {
            var provider = await storageManager.GetProviderAsync(providerId);

            if (!ProviderSupportsBucketManagement(provider.ProviderType))
            {
                logger.LogWarning("Provider type does not support bucket management: {ProviderType}",
                    provider.ProviderType);
                return [];
            }

            if (!Guid.TryParse(providerId, out var providerGuid))
                return [];
            var dbBuckets = (await unitOfWork.StorageBuckets.GetBucketsByProviderIdAsync(providerGuid)).ToList();

            var providerBuckets = (await provider.ListBucketsAsync()).ToList();

            var result = dbBuckets
                .Where(db =>
                {
                    var searchKey = provider.GetBucketSearchKey(db);
                    return providerBuckets.Any(pb => pb.Path == searchKey);
                })
                .Select(db =>
                {
                    var searchKey = provider.GetBucketSearchKey(db);
                    var providerBucket = providerBuckets.FirstOrDefault(pb => pb.Path == searchKey);
                    return new BucketInfoDto
                    {
                        Id = db.Id,
                        Path = db.Path,
                        Description = db.Description,
                        CreationDate = db.CreatedAt,
                        Size = providerBucket?.Size,
                        ObjectCount = providerBucket?.ObjectCount,
                        ProviderType = provider.ProviderType,
                        ProviderName = provider.GetType().Name,
                        ProviderId = providerId
                    };
                })
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing provider buckets: {ProviderId}", providerId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BucketInfoDto>> GetUserAccessibleBucketsForProviderAsync(string userId, string providerId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found: {UserId}", userId);
                return [];
            }

            // Admin users can see all buckets
            var authService = serviceProvider.GetRequiredService<IAuthorizationService>();
            var authResult = await authService.AuthorizeAsync(
                currentUserService.User, 
                "Bucket.Admin");
            
            if (authResult.Succeeded)
            {
                return await ListProviderBucketsAsync(providerId);
            }

            // Normal users can only see buckets they are authorized for
            if (!Guid.TryParse(providerId, out var providerGuid))
            {
                logger.LogWarning("Invalid provider ID: {ProviderId}", providerId);
                return [];
            }

            var userBuckets = await unitOfWork.StorageBuckets.GetUserAccessibleBucketsAsync(userId);
            var providerBuckets = userBuckets.Where(b => b.ProviderId == providerGuid).ToList();

            var provider = await storageManager.GetProviderAsync(providerId);
            
            var result = providerBuckets.Select(bucket => new BucketInfoDto
            {
                Id = bucket.Id,
                Path = bucket.Path,
                Description = bucket.Description,
                CreationDate = bucket.CreatedAt,
                ProviderType = provider.ProviderType,
                ProviderName = provider.GetType().Name,
                ProviderId = providerId
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user provider buckets: {UserId}, {ProviderId}", userId, providerId);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<BucketInfoDto?> GetDefaultBucketForProviderAsync(string providerId)
    {
        try
        {
            if (!Guid.TryParse(providerId, out var providerGuid))
            {
                logger.LogWarning("Invalid provider ID: {ProviderId}", providerId);
                return null;
            }

            var defaultBucket = await unitOfWork.StorageBuckets.GetDefaultBucketForProviderAsync(providerGuid);
            if (defaultBucket == null)
            {
                logger.LogWarning("Default bucket not found for provider: {ProviderId}", providerId);
                return null;
            }

            var provider = await storageManager.GetProviderAsync(providerId);
            
            return new BucketInfoDto
            {
                Id = defaultBucket.Id,
                Path = defaultBucket.Path,
                Description = defaultBucket.Description,
                CreationDate = defaultBucket.CreatedAt,
                ProviderType = provider.ProviderType,
                ProviderName = provider.GetType().Name,
                ProviderId = providerId
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving default bucket: {ProviderId}", providerId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> BucketExistsAsync(string providerId, string bucketName)
    {
        try
        {
            var provider = await storageManager.GetProviderAsync(providerId);

            if (!ProviderSupportsBucketManagement(provider.ProviderType))
            {
                logger.LogWarning("Provider type does not support bucket checking: {ProviderType}",
                    provider.ProviderType);
                return false;
            }

            return await provider.BucketExistsAsync(bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking bucket existence: {ProviderId}, {BucketName}",
                providerId, bucketName);
            return false;
        }
    }


    /// <inheritdoc/>
    public async Task<bool> RemoveBucketAsync(string providerId, string bucketName, bool force = false)
    {
        try
        {
            var provider = await storageManager.GetProviderAsync(providerId);

            if (!ProviderSupportsBucketManagement(provider.ProviderType))
                throw new InvalidOperationException(
                    $"Provider type does not support bucket deletion: {provider.ProviderType}");

            if (force && !AllowsForceDelete(provider.ProviderType))
                throw new InvalidOperationException(
                    $"Provider type does not support force deletion: {provider.ProviderType}");

            if (bucketName.Contains("/") && !AllowsNestedBuckets(provider.ProviderType))
                throw new InvalidOperationException(
                    $"Provider type does not support nested buckets: {provider.ProviderType}");

            if (!await provider.BucketExistsAsync(bucketName)) return false;

            return await provider.RemoveBucketAsync(bucketName, force);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting bucket: {ProviderId}, {BucketName}",
                providerId, bucketName);
            throw;
        }
    }

    /// <summary>
    /// Gets bucket information for a specific bucket ID
    /// </summary>
    public async Task<StorageBucket> GetBucketByIdAsync(Guid bucketId)
    {
        try
        {
            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId);
            if (bucket == null)
            {
                logger.LogWarning("Bucket not found: {BucketId}", bucketId);
                throw new KeyNotFoundException($"Bucket with ID '{bucketId}' not found.");
            }

            return bucket;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            logger.LogError(ex, "Error retrieving bucket: {BucketId}", bucketId);
            throw;
        }
    }

    /// <summary>
    /// Gets bucket permissions
    /// </summary>
    public async Task<IEnumerable<BucketPermissionDto>> GetBucketPermissionsAsync(Guid bucketId)
    {
        try
        {
            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId);
            if (bucket == null)
            {
                logger.LogWarning("Bucket not found: {BucketId}", bucketId);
                return [];
            }

            var permissions = await unitOfWork.BucketPermissions.GetPermissionsByBucketIdAsync(bucketId);

            var permissionDtos = new List<BucketPermissionDto>();
            foreach (var permission in permissions)
            {
                var dto = mapper.Map<BucketPermissionDto>(permission);
                dto.BucketPath = bucket.Path;
                dto.GrantedAt = permission.CreatedAt;

                switch (permission.SubjectType)
                {
                    case Shared.Enums.PermissionSubjectType.User:
                    {
                        var user = await userManager.FindByIdAsync(permission.SubjectId);
                        dto.SubjectName = user?.UserName ?? "Unknown User";
                        break;
                    }
                    case Shared.Enums.PermissionSubjectType.Role:
                    {
                        var role = await roleManager.FindByIdAsync(permission.SubjectId);
                        if (role == null)
                        {
                            logger.LogWarning("Role not found by ID. ID: {RoleId}, bucket: {BucketId}",
                                permission.SubjectId, bucketId);

                            try
                            {
                                role = await roleManager.FindByNameAsync(permission.SubjectId);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error searching by role name: {RoleId}", permission.SubjectId);
                            }
                        }

                        dto.SubjectName = role?.Name ?? $"{permission.SubjectId} (Role)";
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(permission.CreatedBy))
                {
                    var grantedByUser = await userManager.FindByIdAsync(permission.CreatedBy);
                    dto.GrantedByName = grantedByUser?.UserName ?? "Unknown User";
                }

                permissionDtos.Add(dto);
            }

            return permissionDtos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving bucket permissions: {BucketId}", bucketId);
            return [];
        }
    }

    /// <summary>
    /// Gets all bucket information accessible to the user
    /// </summary>
    public async Task<PagedDto<BucketInfoDto>> GetUserAccessiblePaginatedBucketsAsync(string userId, int page,
        int pageSize)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                logger.LogWarning("User not found: {UserId}", userId);
                return new PagedDto<BucketInfoDto>
                {
                    Items = [],
                    TotalCount = 0,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }

            var isAdmin = await userManager.IsInRoleAsync(user, "Admin") || await userManager.IsInRoleAsync(user, "Manager");
            if (isAdmin)
            {
                var (allBuckets, totalCount) = await unitOfWork.StorageBuckets.GetPaginatedBucketsAsync(page, pageSize);
                return new PagedDto<BucketInfoDto>
                {
                    Items = allBuckets.Select(b => MapBucketToInfoDto(b, Shared.Enums.PermissionLevel.Admin)).ToList(),
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }

            var userPermissions = await unitOfWork.BucketPermissions.GetUserPermissionsAsync(userId);

            var userRoles = await userManager.GetRolesAsync(user);

            var rolePermissions = new List<BucketPermission>();
            foreach (var roleName in userRoles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var permissions = await unitOfWork.BucketPermissions.GetRolePermissionsAsync(role.Id);
                    rolePermissions.AddRange(permissions);
                }
            }

            var permissionsByBucket = userPermissions.Concat(rolePermissions)
                .GroupBy(p => p.BucketId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(p => p.Permission));

            var totalAccessibleBuckets = permissionsByBucket.Count;
            var bucketIds = permissionsByBucket.Keys.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = new List<BucketInfoDto>();
            foreach (var bucketId in bucketIds)
            {
                var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId);
                if (bucket != null)
                {
                    var permission = permissionsByBucket[bucketId];
                    var bucketInfo = MapBucketToInfoDto(bucket, permission);
                    result.Add(bucketInfo);
                }
            }

            return new PagedDto<BucketInfoDto>
            {
                Items = result,
                TotalCount = totalAccessibleBuckets,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalAccessibleBuckets / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving user accessible buckets: {UserId}, Page {Page}, Page Size {PageSize}",
                userId, page, pageSize);
            return new PagedDto<BucketInfoDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = 0
            };
        }
    }

    /// <inheritdoc/>
    public async Task<PagedDto<BucketPermissionDto>> GetUserBucketPermissionsPaginatedAsync(string userId, int page,
        int pageSize)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("Permission query made with invalid user ID");
                return new PagedDto<BucketPermissionDto>
                {
                    Items = [],
                    TotalCount = 0,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }

            var userPermissions =
                await unitOfWork.BucketPermissions.GetUserPermissionsPaginatedAsync(userId, page, pageSize);
            var totalUserPermissions = await unitOfWork.BucketPermissions.CountUserPermissionsAsync(userId);

            var permissionDtos = new List<BucketPermissionDto>();
            foreach (var permission in userPermissions)
            {
                var dto = mapper.Map<BucketPermissionDto>(permission);

                if (permission.Bucket != null)
                {
                    dto.BucketPath = permission.Bucket.Path;
                }
                else
                {
                    var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(permission.BucketId);
                    if (bucket != null) dto.BucketPath = bucket.Path;
                }

                switch (permission.SubjectType)
                {
                    case Shared.Enums.PermissionSubjectType.User:
                    {
                        var permissionUser = await userManager.FindByIdAsync(permission.SubjectId);
                        dto.SubjectName = permissionUser?.UserName ?? "Unknown User";
                        break;
                    }
                    case Shared.Enums.PermissionSubjectType.Role:
                    {
                        var role = await roleManager.FindByIdAsync(permission.SubjectId);
                        dto.SubjectName = role?.Name ?? "Unknown Role";
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(permission.CreatedBy))
                {
                    var grantedByUser = await userManager.FindByIdAsync(permission.CreatedBy);
                    dto.GrantedByName = grantedByUser?.UserName ?? "Unknown User";
                }

                permissionDtos.Add(dto);
            }

            var totalPages = (int)Math.Ceiling(totalUserPermissions / (double)pageSize);

            return new PagedDto<BucketPermissionDto>
            {
                Items = permissionDtos,
                TotalCount = totalUserPermissions,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing user bucket permissions: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PagedDto<BucketPermissionDto>> GetAllBucketPermissionsPaginatedAsync(int page, int pageSize)
    {
        try
        {
            var permissions = await unitOfWork.BucketPermissions.GetAllPermissionsPaginatedAsync(page, pageSize);
            var totalPermissions = await unitOfWork.BucketPermissions.CountAllPermissionsAsync();

            var permissionDtos = new List<BucketPermissionDto>();
            foreach (var permission in permissions)
            {
                var dto = mapper.Map<BucketPermissionDto>(permission);

                if (permission.Bucket != null)
                {
                    dto.BucketPath = permission.Bucket.Path;
                }
                else
                {
                    var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(permission.BucketId);
                    if (bucket != null) dto.BucketPath = bucket.Path;
                }

                if (permission.SubjectType == Shared.Enums.PermissionSubjectType.User)
                {
                    var permissionUser = await userManager.FindByIdAsync(permission.SubjectId);
                    dto.SubjectName = permissionUser?.UserName ?? "Unknown User";
                }
                else if (permission.SubjectType == Shared.Enums.PermissionSubjectType.Role)
                {
                    var permissionRole = await roleManager.FindByIdAsync(permission.SubjectId);
                    dto.SubjectName = permissionRole?.Name ?? "Unknown Role";
                }

                if (!string.IsNullOrEmpty(permission.CreatedBy))
                {
                    var grantedByUser = await userManager.FindByIdAsync(permission.CreatedBy);
                    dto.GrantedByName = grantedByUser?.UserName ?? "Unknown User";
                }

                permissionDtos.Add(dto);
            }

            var totalPages = (int)Math.Ceiling(totalPermissions / (double)pageSize);

            return new PagedDto<BucketPermissionDto>
            {
                Items = permissionDtos,
                TotalCount = totalPermissions,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing all bucket permissions");
            throw;
        }
    }

    /// <summary>
    /// Converts StorageBucket to BucketInfoDto
    /// </summary>
    private BucketInfoDto MapBucketToInfoDto(StorageBucket bucket, Shared.Enums.PermissionLevel permissionLevel)
    {
        var providerTask = unitOfWork.StorageProviders.GetByIdAsync(bucket.ProviderId);
        providerTask.Wait();
        var provider = providerTask.Result;

        return new BucketInfoDto
        {
            Id = bucket.Id,
            Path = bucket.Path,
            Description = bucket.Description,
            Permission = permissionLevel,
            CreationDate = bucket.CreatedAt,
            Size = null,
            ObjectCount = null,
            ProviderId = bucket.ProviderId.ToString(),
            ProviderName = provider?.Name ?? "Unknown Provider",
            ProviderType = provider?.ProviderType ?? "Unknown"
        };
    }

    /// <summary>
    /// Gets paginated bucket list
    /// </summary>
    public async Task<IEnumerable<StorageBucket>> GetPaginatedBucketsAsync(int page, int pageSize)
    {
        try
        {
            return await unitOfWork.StorageBuckets.GetPaginatedAsync(page, pageSize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving buckets: Page {Page}, Page Size {PageSize}", page, pageSize);
            return [];
        }
    }

    /// <summary>
    /// Gets bucket ID by provider ID and bucket path
    /// </summary>
    public async Task<Guid?> GetBucketIdByProviderAndPathAsync(string providerId, string bucketPath)
    {
        try
        {
            var bucket = await unitOfWork.StorageBuckets.GetByProviderAndPathAsync(providerId, bucketPath);
            return bucket?.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting bucket ID by provider and path: {ProviderId}, {BucketPath}",
                providerId, bucketPath);
            return null;
        }
    }

    /// <summary>
    /// Creates a new bucket (detailed)
    /// </summary>
    public async Task<StorageBucket> CreateBucketAsync(BucketCreateDto dto, string userId)
    {
        try
        {
            var provider = await storageManager.GetProviderAsync(dto.ProviderId);

            if (!ProviderSupportsBucketManagement(provider.ProviderType))
                throw new InvalidOperationException(
                    $"Provider type '{provider.ProviderType}' does not support bucket management.");

            var (isValid, message) = ValidateUniversalBucketName(dto.BucketPath);
            if (!isValid) 
            {
                var suggestion = SuggestValidBucketName(dto.BucketPath);
                throw new ArgumentException($"{message} Suggested name: '{suggestion}'");
            }

            var (providerValid, providerMessage) = ValidateBucketName(provider.ProviderType, dto.BucketPath);
            if (!providerValid) throw new ArgumentException(providerMessage);

            if (dto.BucketPath.Contains('/') && !AllowsNestedBuckets(provider.ProviderType))
                throw new ArgumentException(
                    $"Provider type '{provider.ProviderType}' does not support nested bucket names.");

            var existingBucket = await unitOfWork.StorageBuckets.GetByProviderAndPathAsync(dto.ProviderId, dto.BucketPath);
            if (existingBucket != null)
                throw new InvalidOperationException(
                    $"A bucket is already registered in this provider at path '{dto.BucketPath}'. Please choose a different path.");

            var bucketExists = await provider.BucketExistsAsync(dto.BucketPath);
            if (bucketExists)
                throw new InvalidOperationException(
                    $"A bucket already exists in the storage provider at path '{dto.BucketPath}'. Please choose a different path.");

            var bucketInfo = await provider.CreateBucketAsync(dto.BucketPath);

            if (!bucketInfo)
                throw new InvalidOperationException(
                    $"Bucket '{dto.BucketPath}' could not be created. A problem occurred in the storage provider.");

            // Validate ProviderId format
            if (!Guid.TryParse(dto.ProviderId, out var providerGuid))
            {
                throw new ArgumentException($"Invalid provider ID format: '{dto.ProviderId}'. Must be a valid GUID.", nameof(dto.ProviderId));
            }

            var bucket = new StorageBucket
            {
                Id = Guid.NewGuid(),
                Path = dto.BucketPath,
                Description = dto.Description,
                ProviderId = providerGuid,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                IsPublic = dto.IsPublic,
                AllowDirectAccess = dto.AllowDirectAccess
            };

            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.StorageBuckets.AddAsync(bucket);

                await permissionManager.AssignPermissionWithoutTransactionAsync(
                    bucket.Id,
                    userId,
                    Shared.Enums.PermissionSubjectType.User,
                    Shared.Enums.PermissionLevel.Admin,
                    userId);

                return bucket;
            });
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            if (ex.Message.Contains("duplicate") || ex.Message.Contains("unique") || ex.Message.Contains("UNIQUE"))
            {
                logger.LogWarning("Duplicate bucket path detected: {ProviderId}, {BucketPath}", dto.ProviderId, dto.BucketPath);
                throw new InvalidOperationException($"A bucket already exists at path '{dto.BucketPath}'. Please choose a different path.");
            }
            
            logger.LogError(ex, "Error creating bucket: {ProviderId}, {BucketPath}",
                dto.ProviderId, dto.BucketPath);

            // Retry cleanup mechanism to prevent orphaned buckets
            await RetryBucketCleanupAsync(dto.ProviderId, dto.BucketPath, maxRetries: 3);

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetBucketPathByIdAsync(Guid bucketId)
    {
        try
        {
            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId);
            return bucket?.Path;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting bucket path from bucket ID: {BucketId}", bucketId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetBucketIdByPathAsync(string bucketPath)
    {
        try
        {
            if (string.IsNullOrEmpty(bucketPath)) return null;

            var bucket = await unitOfWork.StorageBuckets.FindSingleAsync(b => b.Path == bucketPath);
            return bucket?.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting bucket ID from bucket path: {BucketPath}", bucketPath);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasDocumentsAsync(Guid bucketId)
    {
        try
        {
            // Check if there are documents belonging to this bucket in the database
            var hasDocuments = await unitOfWork.Documents.ExistsAsync(d => d.BucketId == bucketId);
            
            logger.LogInformation("Bucket {BucketId} document check: {HasDocuments}", bucketId, hasDocuments);
            
            return hasDocuments;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking bucket documents: {BucketId}", bucketId);
            return true; // Return true for security - if it cannot be checked, do not allow deletion
        }
    }

    /// <summary>
    /// Universal bucket name validation for all providers
    /// Follows the most restrictive rules (S3/MinIO compatible)
    /// </summary>
    public static (bool isValid, string message) ValidateUniversalBucketName(string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            return (false, "Bucket name cannot be empty");

        if (bucketName.Length < 3)
            return (false, "Bucket name must be at least 3 characters long");

        if (bucketName.Length > 63)
            return (false, "Bucket name cannot exceed 63 characters");

        if (!Regex.IsMatch(bucketName, @"^[a-z0-9][a-z0-9\-]{1,61}[a-z0-9]$"))
            return (false, "Bucket name can only contain lowercase letters (a-z), numbers (0-9), and hyphens (-). Must start and end with a letter or number");

        if (bucketName.StartsWith("xn--") || bucketName.StartsWith("sthree-"))
            return (false, "Bucket name cannot start with 'xn--' or 'sthree-'");

        if (Regex.IsMatch(bucketName, @"^(\d{1,3}\.){3}\d{1,3}$"))
            return (false, "Bucket name cannot be in IP address format");

        if (bucketName.Contains("--"))
            return (false, "Bucket name cannot contain consecutive hyphens");

        string[] reservedNames = ["con", "prn", "aux", "nul"];
        if (reservedNames.Contains(bucketName.ToLower()))
            return (false, $"'{bucketName}' is a reserved name");

        return (true, "Valid bucket name");
    }

    /// <summary>
    /// Suggests a valid bucket name based on invalid input
    /// </summary>
    public static string SuggestValidBucketName(string invalidName)
    {
        if (string.IsNullOrWhiteSpace(invalidName))
            return "default-bucket";

        // Convert to lowercase
        var suggested = invalidName.ToLowerInvariant();

        // Replace invalid characters with hyphens
        suggested = Regex.Replace(suggested, @"[^a-z0-9\-]", "-");

        // Remove consecutive hyphens
        suggested = Regex.Replace(suggested, @"-+", "-");

        // Ensure starts and ends with alphanumeric
        suggested = suggested.Trim('-');
        if (string.IsNullOrEmpty(suggested) || !char.IsLetterOrDigit(suggested[0]))
            suggested = "bucket-" + suggested;
        if (!char.IsLetterOrDigit(suggested[suggested.Length - 1]))
            suggested = suggested + "-1";

        // Ensure length constraints
        if (suggested.Length < 3)
            suggested = suggested.PadRight(3, '0');
        if (suggested.Length > 63)
            suggested = suggested.Substring(0, 60) + "-01";

        return suggested;
    }

    #region Helper Methods

    private bool ProviderSupportsBucketManagement(string providerType)
    {
        return _providerConfigs.ContainsKey(providerType);
    }

    private (bool isValid, string message) ValidateBucketName(string providerType, string bucketName)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            return (false, "Bucket name cannot be empty.");

        if (_providerConfigs.TryGetValue(providerType, out var config)) return config.ValidateBucketName(bucketName);

        return (true, "");
    }

    private bool AllowsNestedBuckets(string providerType)
    {
        return _providerConfigs.TryGetValue(providerType, out var config) && config.AllowNestedBuckets;
    }

    private bool AllowsForceDelete(string providerType)
    {
        return _providerConfigs.TryGetValue(providerType, out var config) && config.AllowForceDelete;
    }

    /// <summary>
    /// Retries bucket cleanup to prevent orphaned resources
    /// </summary>
    private async Task RetryBucketCleanupAsync(string providerId, string bucketPath, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var provider = await storageManager.GetProviderAsync(providerId);
                await provider.RemoveBucketAsync(bucketPath);
                
                logger.LogInformation("Successfully cleaned up bucket on attempt {Attempt}: {ProviderId}, {BucketPath}",
                    attempt, providerId, bucketPath);
                return; // Success, exit retry loop
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning(cleanupEx, "Bucket cleanup attempt {Attempt}/{MaxRetries} failed: {ProviderId}, {BucketPath}",
                    attempt, maxRetries, providerId, bucketPath);

                if (attempt == maxRetries)
                {
                    // Final attempt failed - log as error and potentially queue for manual cleanup
                    logger.LogError("CRITICAL: Bucket cleanup failed after {MaxRetries} attempts. " +
                                  "Manual cleanup required for orphaned bucket: {ProviderId}, {BucketPath}. " +
                                  "This may result in additional storage costs.",
                                  maxRetries, providerId, bucketPath);
                    
                    // TODO: In future, add to dead letter queue for manual cleanup
                    break;
                }

                // Exponential backoff: 1s, 2s, 4s
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                await Task.Delay(delay);
            }
        }
    }

    #endregion
}

/// <summary>
/// Bucket provider configuration for each provider type
/// </summary>
internal class BucketProviderConfig
{
    public int MinBucketNameLength { get; set; }
    public int MaxBucketNameLength { get; set; }
    public required string AllowedCharactersPattern { get; set; }
    public bool AllowNestedBuckets { get; set; }
    public bool RequiresPermissionCheck { get; set; }
    public bool AllowForceDelete { get; set; }
    public required Func<string, (bool isValid, string message)> ValidateBucketName { get; set; }
}
