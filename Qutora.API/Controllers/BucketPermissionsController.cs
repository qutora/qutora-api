using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Common;
using Qutora.Shared.Enums;
using System.Security.Claims;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;

namespace Qutora.API.Controllers;

[Authorize]
[ApiController]
[Route("api/storage/permissions")]
public class BucketPermissionsController : ControllerBase
{
    private readonly IBucketPermissionManager _permissionManager;
    private readonly IStorageBucketService _bucketService;
    private readonly IUserService _userService;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<BucketPermissionsController> _logger;
    private readonly IAuthorizationService _authorizationService;

    public BucketPermissionsController(
        IBucketPermissionManager permissionManager,
        IStorageBucketService bucketService,
        IUserService userService,
        IApiKeyService apiKeyService,
        ILogger<BucketPermissionsController> logger,
        IAuthorizationService authorizationService)
    {
        _permissionManager = permissionManager;
        _bucketService = bucketService;
        _userService = userService;
        _apiKeyService = apiKeyService;
        _logger = logger;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Checks if a user has permission to operate on a bucket
    /// </summary>
    [HttpGet("check/{bucketId}")]
    public async Task<IActionResult> CheckPermission(Guid bucketId, [FromQuery] PermissionLevel requiredPermission)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "You need to be logged in." });

            var result = await _permissionManager.CheckUserBucketOperationPermissionAsync(
                userId, bucketId, requiredPermission);

            return Ok(new
            {
                isAllowed = result.IsAllowed,
                reason = result.DeniedReason,
                requiredPermission = result.RequiredPermission,
                userPermission = result.UserPermission
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during permission check: {BucketId}, {RequiredPermission}",
                bucketId, requiredPermission);
            return StatusCode(500, new { message = "An error occurred while checking permissions." });
        }
    }

    /// <summary>
    /// Lists all permissions for a specific bucket
    /// </summary>
    [HttpGet("bucket/{bucketId}")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> GetBucketPermissions(Guid bucketId)
    {
        try
        {
            var permissions = await _bucketService.GetBucketPermissionsAsync(bucketId);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing bucket permissions: {BucketId}", bucketId);
            return StatusCode(500, new { message = "An error occurred while retrieving bucket permissions." });
        }
    }

    /// <summary>
    /// Lists all permissions for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<ActionResult<PagedDto<BucketPermissionDto>>> GetUserPermissions(string userId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var permissions = await _bucketService.GetUserBucketPermissionsPaginatedAsync(userId, page, pageSize);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing user permissions: {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving user permissions." });
        }
    }

    /// <summary>
    /// Lists a user's own bucket permissions
    /// </summary>
    [HttpGet("my-permissions")]
    public async Task<ActionResult<PagedDto<BucketPermissionDto>>> GetMyPermissions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { message = "You need to be logged in." });

            var permissions = await _bucketService.GetUserBucketPermissionsPaginatedAsync(userId, page, pageSize);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogError(ex, "Error listing user permissions: {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving your permissions." });
        }
    }

    /// <summary>
    /// Assigns a new permission to a bucket
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> GrantPermission([FromBody] BucketPermissionCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var grantedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var permission = await _permissionManager.AssignPermissionAsync(
                dto.BucketId,
                dto.SubjectId,
                dto.SubjectType,
                dto.Permission,
                grantedBy);

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission: {BucketId}, {SubjectId}, {SubjectType}",
                dto.BucketId, dto.SubjectId, dto.SubjectType);
            return StatusCode(500, new { message = "An error occurred while assigning permission." });
        }
    }

    /// <summary>
    /// Removes a permission from a bucket
    /// </summary>
    [HttpDelete("{permissionId}")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> RevokePermission(Guid permissionId)
    {
        try
        {
            await _permissionManager.RemovePermissionAsync(permissionId);
            return Ok(new { success = true, message = "Permission successfully removed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission: {PermissionId}", permissionId);
            return StatusCode(500, new { message = "An error occurred while removing permission." });
        }
    }

    /// <summary>
    /// Assigns an API Key permission to a bucket
    /// </summary>
    [HttpPost("api-key")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> GrantApiKeyPermission([FromBody] ApiKeyBucketPermissionCreateDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var grantedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var permission = await _permissionManager.AssignApiKeyPermissionAsync(
                dto.ApiKeyId,
                dto.BucketId,
                dto.Permission,
                grantedBy);

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning API Key permission: {ApiKeyId}, {BucketId}",
                dto.ApiKeyId, dto.BucketId);
            return StatusCode(500, new { message = "An error occurred while assigning API Key permission." });
        }
    }

    /// <summary>
    /// Removes an API Key permission from a bucket
    /// </summary>
    [HttpDelete("api-key/{permissionId}")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> RevokeApiKeyPermission(Guid permissionId)
    {
        try
        {
            await _permissionManager.RemoveApiKeyPermissionAsync(permissionId);
            return Ok(new { success = true, message = "API Key permission successfully removed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing API Key permission: {PermissionId}", permissionId);
            return StatusCode(500, new { message = "An error occurred while removing API Key permission." });
        }
    }

    /// <summary>
    /// Lists all permissions for a specific API Key
    /// </summary>
    [HttpGet("api-key/{apiKeyId}")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> GetApiKeyPermissions(Guid apiKeyId)
    {
        try
        {
            var permissions = await _apiKeyService.GetApiKeyBucketPermissionsAsync(apiKeyId);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing API Key permissions: {ApiKeyId}", apiKeyId);
            return StatusCode(500, new { message = "An error occurred while retrieving API Key permissions." });
        }
    }

    /// <summary>
    /// Lists all user permissions (Admin only)
    /// </summary>
    [HttpPost("user")]
    [Authorize(Policy = "BucketPermission.Manage")]
    public async Task<IActionResult> GetAllUserPermissions([FromBody] PageRequestDto requestDto)
    {
        try
        {
            if (requestDto == null) requestDto = new PageRequestDto { PageNumber = 1, PageSize = 20 };

            var hasAdminAccess = await _authorizationService.AuthorizeAsync(User, "Admin.Access");
            if (!hasAdminAccess.Succeeded) return Forbid();

            var permissions = await _bucketService.GetAllBucketPermissionsPaginatedAsync(
                requestDto.PageNumber, requestDto.PageSize);

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing all user permissions");
            return StatusCode(500, new { message = "An error occurred while retrieving user permissions." });
        }
    }
}
