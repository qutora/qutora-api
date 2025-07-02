using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Common;
using Qutora.Shared.Enums;
using System.Security.Claims;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Exceptions;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.Storage;

namespace Qutora.API.Controllers;

[Authorize]
[ApiController]
[Route("api/storage/buckets")]
public class StorageBucketsController : ControllerBase
{
    private readonly IStorageBucketService _bucketService;
    private readonly IStorageManager _storageManager;
    private readonly IBucketPermissionManager _permissionManager;
    private readonly ILogger<StorageBucketsController> _logger;
    private readonly IAuthorizationService _authorizationService;

    public StorageBucketsController(
        IStorageBucketService bucketService,
        IStorageManager storageManager,
        IBucketPermissionManager permissionManager,
        ILogger<StorageBucketsController> logger,
        IAuthorizationService authorizationService)
    {
        _bucketService = bucketService;
        _storageManager = storageManager;
        _permissionManager = permissionManager;
        _logger = logger;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Lists buckets/folders in a specific storage provider (filtered by user permissions)
    /// </summary>
    [HttpGet("provider/{providerId}")]
    [Authorize(Policy = "StorageProvider.Read")]
    public async Task<IActionResult> GetProviderBuckets(string providerId)
    {
        try
        {
            var provider = await GetProviderAndCheckCapability(providerId, StorageCapability.BucketListing);
            if (provider == null)
                return BadRequest(new ErrorResponse
                {
                    Code = "UnsupportedOperation",
                    Message = "This storage provider does not support bucket listing."
                });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ErrorResponse
                {
                    Code = "Unauthorized",
                    Message = "User ID not found in token"
                });
            }

            // Admins can see all buckets
            var authResult = await _authorizationService.AuthorizeAsync(User, "Bucket.Admin");
            if (authResult.Succeeded)
            {
                var allBuckets = await _bucketService.ListProviderBucketsAsync(providerId);
                return Ok(allBuckets);
            }

            // Regular users can only see buckets they have permission for
            var userBuckets = await _bucketService.GetUserAccessibleBucketsForProviderAsync(userId, providerId);
            return Ok(userBuckets);
        }
        catch (ProviderNotFoundException ex)
        {
            _logger.LogWarning(ex, "Provider not found: {ProviderId}", providerId);
            return NotFound(new ErrorResponse
            {
                Code = "ProviderNotFound",
                Message = $"Storage provider not found: {providerId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing provider buckets: {ProviderId}", providerId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while retrieving bucket list.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Lists all buckets the user has access to
    /// </summary>
    [HttpGet("my-accessible-buckets")]
    [Authorize(Policy = "Bucket.UserAccess")]
    public async Task<ActionResult<PagedDto<BucketInfoDto>>> GetMyAccessibleBuckets([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ErrorResponse
                {
                    Code = "Unauthorized",
                    Message = "You need to be logged in."
                });

            var buckets = await _bucketService.GetUserAccessiblePaginatedBucketsAsync(userId, page, pageSize);

            return Ok(buckets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing accessible buckets");
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while listing accessible buckets.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Checks if a bucket/folder exists
    /// </summary>
    [HttpGet("exists")]
    [Authorize(Policy = "StorageProvider.Read")]
    public async Task<IActionResult> BucketExists([FromQuery] string providerId, [FromQuery] string bucketPath)
    {
        try
        {
            if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(bucketPath))
                return BadRequest(new ErrorResponse
                {
                    Code = "MissingParameter",
                    Message = "Provider ID and bucket path parameters are required."
                });

            var provider = await GetProviderAndCheckCapability(providerId, StorageCapability.BucketExistence);
            if (provider == null)
                return BadRequest(new ErrorResponse
                {
                    Code = "UnsupportedOperation",
                    Message = "This storage provider does not support bucket existence check."
                });

            if (bucketPath.Contains('/') && !SupportsNestedBuckets(provider.ProviderType))
                return BadRequest(new ErrorResponse
                {
                    Code = "UnsupportedOperation",
                    Message = $"Provider type '{provider.ProviderType}' does not support nested bucket paths."
                });

            var exists = await _bucketService.BucketExistsAsync(providerId, bucketPath);
            return Ok(new { exists });
        }
        catch (ProviderNotFoundException ex)
        {
            _logger.LogWarning(ex, "Provider not found: {ProviderId}", providerId);
            return NotFound(new ErrorResponse
            {
                Code = "ProviderNotFound",
                Message = $"Storage provider not found: {providerId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bucket existence: {ProviderId}, {BucketPath}", providerId, bucketPath);
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while checking bucket existence.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Creates a new bucket/folder
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "StorageProvider.Create")]
    public async Task<IActionResult> CreateBucket([FromBody] BucketCreateDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.ProviderId) || string.IsNullOrEmpty(dto.BucketPath))
                return BadRequest(new ErrorResponse
                {
                    Code = "MissingParameter",
                    Message = "Provider ID and bucket path are required."
                });

            var provider = await GetProviderAndCheckCapability(dto.ProviderId, StorageCapability.BucketCreation);
            if (provider == null)
                return BadRequest(new ErrorResponse
                {
                    Code = "UnsupportedOperation",
                    Message = "This storage provider does not support bucket creation."
                });

            if (dto.BucketPath.Contains('/') && !SupportsNestedBuckets(provider.ProviderType))
                return BadRequest(new ErrorResponse
                {
                    Code = "UnsupportedOperation",
                    Message = $"Provider type '{provider.ProviderType}' does not support nested bucket paths."
                });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bucket = await _bucketService.CreateBucketAsync(dto, userId);

            if (!string.IsNullOrEmpty(userId))
                await _permissionManager.AssignPermissionAsync(
                    bucket.Id,
                    userId,
                    PermissionSubjectType.User,
                    PermissionLevel.Admin,
                    userId);

            return Ok(bucket);
        }
        catch (ProviderNotFoundException ex)
        {
            _logger.LogWarning(ex, "Provider not found: {ProviderId}", dto.ProviderId);
            return NotFound(new ErrorResponse
            {
                Code = "ProviderNotFound",
                Message = $"Storage provider not found: {dto.ProviderId}"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bucket creation error: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                Code = "InvalidOperation",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bucket");
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while creating bucket.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Deletes a bucket by ID (only if empty)
    /// </summary>
    [HttpDelete("{bucketId}")]
    [Authorize(Policy = "StorageProvider.Delete")]
    public async Task<IActionResult> RemoveBucket(Guid bucketId)
    {
        try
        {
            var bucket = await _bucketService.GetBucketByIdAsync(bucketId);
            if (bucket == null)
                return NotFound(new ErrorResponse
                {
                    Code = "BucketNotFound",
                    Message = "Bucket not found."
                });

            var provider = await GetProviderAndCheckCapability(bucket.ProviderId.ToString(), StorageCapability.BucketDeletion);
            if (provider == null)
                return BadRequest(new ErrorResponse
                {
                    Code = "UnsupportedOperation",
                    Message = "This storage provider does not support bucket deletion."
                });

            if (bucket.Path.Contains('/') && !SupportsNestedBuckets(provider.ProviderType))
                return BadRequest(new ErrorResponse
                {
                    Code = "UnsupportedOperation",
                    Message = $"Provider type '{provider.ProviderType}' does not support nested bucket paths."
                });

            // Permission check
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAdminAccess = await _authorizationService.AuthorizeAsync(User, "Admin.Access");
            var hasBucketManage = await _authorizationService.AuthorizeAsync(User, "Bucket.Manage");
            
            if (!hasAdminAccess.Succeeded && !hasBucketManage.Succeeded)
            {
                var isAdmin = await _permissionManager.IsUserBucketAdminAsync(userId, bucketId);
                if (!isAdmin) return Forbid();
            }

            // DEFAULT BUCKET SİLME KORUMASII - KRİTİK GÜVENLİK KONTROLÜ
            if (bucket.IsDefault)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = "DefaultBucketProtected",
                    Message = "Default bucket cannot be deleted. This bucket is required for system operation."
                });
            }

            // Check if bucket has any documents in database
            var hasDocuments = await _bucketService.HasDocumentsAsync(bucketId);
            if (hasDocuments)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = "BucketNotEmpty",
                    Message = "Bucket contains documents and cannot be deleted. Please delete all documents first."
                });
            }

            // Check if bucket is empty in storage provider
            var deleteResult = await _bucketService.RemoveBucketAsync(bucket.ProviderId.ToString(), bucket.Path, false);
            
            if (!deleteResult)
            {
                return BadRequest(new ErrorResponse
                {
                    Code = "BucketNotEmpty",
                    Message = "Bucket is not empty in storage provider. Please delete all files first."
                });
            }

            // Remove all permissions after successful deletion
            await _permissionManager.RemoveAllPermissionsForBucketAsync(bucketId);

            return Ok(new
            {
                success = true,
                message = $"Bucket '{bucket.Path}' successfully deleted."
            });
        }
        catch (ProviderNotFoundException ex)
        {
            _logger.LogWarning(ex, "Provider not found for bucket: {BucketId}", bucketId);
            return NotFound(new ErrorResponse
            {
                Code = "ProviderNotFound",
                Message = "Storage provider not found for this bucket."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bucket deletion error: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                Code = "InvalidOperation",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bucket: {BucketId}", bucketId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while deleting bucket.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets details of a specific bucket
    /// </summary>
    [HttpGet("{bucketId}")]
    public async Task<IActionResult> GetBucketById(Guid bucketId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAdminAccess = await _authorizationService.AuthorizeAsync(User, "Admin.Access");
            var hasBucketManage = await _authorizationService.AuthorizeAsync(User, "Bucket.Manage");
            
            if (!hasAdminAccess.Succeeded && !hasBucketManage.Succeeded)
            {
                var isAdmin = await _permissionManager.IsUserBucketAdminAsync(userId, bucketId);
                if (!isAdmin) return Forbid();
            }

            var bucket = await _bucketService.GetBucketByIdAsync(bucketId);
            if (bucket == null)
                return NotFound(new ErrorResponse
                {
                    Code = "BucketNotFound",
                    Message = "Bucket not found."
                });

            return Ok(bucket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bucket details: {BucketId}", bucketId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while retrieving bucket details.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets bucket permissions
    /// </summary>
    [HttpGet("{bucketId}/permissions")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> GetBucketPermissions(Guid bucketId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var bucket = await _bucketService.GetBucketByIdAsync(bucketId);
            if (bucket == null)
                return NotFound(new ErrorResponse
                {
                    Code = "BucketNotFound",
                    Message = "Bucket not found."
                });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAdminAccess = await _authorizationService.AuthorizeAsync(User, "Admin.Access");
            var hasBucketManage = await _authorizationService.AuthorizeAsync(User, "Bucket.Manage");
            
            if (!hasAdminAccess.Succeeded && !hasBucketManage.Succeeded)
            {
                var isAdmin = await _permissionManager.IsUserBucketAdminAsync(userId, bucketId);
                if (!isAdmin) return Forbid();
            }

            var permissions = await _bucketService.GetBucketPermissionsAsync(bucketId);

            var paginatedPermissions = permissions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                Items = paginatedPermissions,
                TotalCount = permissions.Count(),
                Page = page,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bucket permissions: {BucketId}", bucketId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while retrieving bucket permissions.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Lists all bucket user permissions (for Admin)
    /// </summary>
    [HttpGet("user-permissions")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> GetAllUserBucketPermissions([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ErrorResponse
                {
                    Code = "Unauthorized",
                    Message = "You need to be logged in."
                });

            var hasAdminAccess = await _authorizationService.AuthorizeAsync(User, "Admin.Access");
            var hasBucketManage = await _authorizationService.AuthorizeAsync(User, "Bucket.Manage");
            
            if (!hasAdminAccess.Succeeded && !hasBucketManage.Succeeded) return Forbid();

            var paginatedPermissions =
                await _bucketService.GetUserBucketPermissionsPaginatedAsync(userId, page, pageSize);

            return Ok(paginatedPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user bucket permissions");
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while retrieving user bucket permissions.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Assigns a new permission for a bucket
    /// </summary>
    [HttpPost("{bucketId}/permissions")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> GrantBucketPermission(Guid bucketId, [FromBody] GrantPermissionDto dto)
    {
        try
        {
            var bucket = await _bucketService.GetBucketByIdAsync(bucketId);
            if (bucket == null)
                return NotFound(new ErrorResponse
                {
                    Code = "BucketNotFound",
                    Message = "Bucket not found."
                });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAdminAccess = await _authorizationService.AuthorizeAsync(User, "Admin.Access");
            var hasBucketManage = await _authorizationService.AuthorizeAsync(User, "Bucket.Manage");
            
            if (!hasAdminAccess.Succeeded && !hasBucketManage.Succeeded)
            {
                var isAdmin = await _permissionManager.IsUserBucketAdminAsync(userId, bucketId);
                if (!isAdmin) return Forbid();
            }

            var permission = await _permissionManager.AssignPermissionAsync(
                bucketId,
                dto.SubjectId,
                dto.SubjectType,
                dto.Permission,
                userId);

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning bucket permission: {BucketId}", bucketId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while assigning bucket permission.",
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Removes a bucket permission
    /// </summary>
    [HttpDelete("permissions/{permissionId}")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> RemoveBucketPermission(Guid permissionId)
    {
        try
        {
            await _permissionManager.RemovePermissionAsync(permissionId);

            return Ok(new
            {
                success = true,
                message = "Bucket permission successfully removed."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing bucket permission: {PermissionId}", permissionId);
            return StatusCode(500, new ErrorResponse
            {
                Code = "InternalError",
                Message = "An error occurred while removing bucket permission.",
                Details = ex.Message
            });
        }
    }

    #region Helper Methods

    private async Task<IStorageProvider> GetProviderAndCheckCapability(string providerId, StorageCapability capability)
    {
        var provider = await _storageManager.GetProviderAsync(providerId);
        if (provider == null) throw new ProviderNotFoundException(providerId);

        if (!provider.SupportsCapability(capability)) return null;

        return provider;
    }

    private bool SupportsNestedBuckets(string providerType)
    {
        return providerType.Equals("FileSystem", StringComparison.OrdinalIgnoreCase) ||
               providerType.Equals("FTP", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
