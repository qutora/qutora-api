using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Application.Models.ApiKeys;
using AuthDTOs = Qutora.Shared.DTOs.Authentication;
using System.Security.Claims;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;

namespace Qutora.API.Controllers;

[ApiController]
[Route("api/auth/api-keys")]
[Authorize(Policy = "ApiKey.Manage")]
public class ApiKeysController(
    IApiKeyService apiKeyService,
    ICurrentUserService currentUserService,
    IAuditService auditService,
    ILogger<ApiKeysController> logger,
    IAuthorizationService authorizationService,
    IStorageProviderService storageProviderService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiKeyResponseDto>>> GetUserApiKeys()
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var apiKeys = await apiKeyService.GetApiKeysByUserIdAsync(userId);

            var response = apiKeys.Select(k => new ApiKeyResponseDto
            {
                Id = k.Id,
                Name = k.Name,
                Key = k.Key,
                CreatedAt = k.CreatedAt,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                IsActive = k.IsActive,
                Permission = k.Permissions.ToString(),
                ProviderCount = k.AllowedProviderIds.Count
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting API keys for user");
            return StatusCode(500, "Error retrieving API keys");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiKeyResponseDto>> GetApiKey(Guid id)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var apiKey = await apiKeyService.GetApiKeyByIdAsync(id);

            var hasAdminAccess = await authorizationService.AuthorizeAsync(User, "Admin.Access");
            if (apiKey.UserId != userId && !hasAdminAccess.Succeeded) return Forbid();

            var response = new ApiKeyResponseDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Key = apiKey.Key,
                CreatedAt = apiKey.CreatedAt,
                ExpiresAt = apiKey.ExpiresAt,
                LastUsedAt = apiKey.LastUsedAt,
                IsActive = apiKey.IsActive,
                Permission = apiKey.Permissions.ToString(),
                ProviderCount = apiKey.AllowedProviderIds.Count
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting API key {Id}", id);
            return StatusCode(500, "Error retrieving API key");
        }
    }

    /// <summary>
    /// Creates a new API key
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] AuthDTOs.CreateApiKeyDto request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User authentication required." });

            // Validate provider access if specific providers are requested
            if (request.AllowedProviderIds != null && request.AllowedProviderIds.Any())
            {
                var userAccessibleProviders = await storageProviderService.GetUserAccessibleProvidersAsync(userId);
                var userProviderIds = userAccessibleProviders.Select(p => p.Id).ToHashSet();

                var invalidProviders = request.AllowedProviderIds.Where(pid => !userProviderIds.Contains(pid)).ToList();
                if (invalidProviders.Any())
                {
                    return Forbid("You can only create API keys for storage providers you have access to.");
                }
            }

            var allowedProviderIds = request.AllowedProviderIds ?? new List<Guid>();

            var result = await apiKeyService.CreateApiKeyAsync(
                userId,
                request.Name,
                request.ExpiresAt,
                allowedProviderIds,
                request.Permission);
            var key = result.Item1;
            var secret = result.Item2;
            var apiKey = result.Item3;

            var response = new
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Key = key,
                Secret = secret,
                ExpiresAt = apiKey.ExpiresAt,
                Permission = apiKey.Permissions.ToString(),
                CreatedAt = apiKey.CreatedAt,
                ProviderCount = apiKey.AllowedProviderIds.Count
            };

            return CreatedAtAction(nameof(GetApiKey), new { id = apiKey.Id }, response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating API key");
            return StatusCode(500, new { message = "An error occurred while creating the API key." });
        }
    }

    /// <summary>
    /// Updates an existing API key
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateApiKey(Guid id, [FromBody] AuthDTOs.UpdateApiKeyDto request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User authentication required." });

            var apiKey = await apiKeyService.GetApiKeyByIdAsync(id);
            if (apiKey == null)
                return NotFound(new { message = "API key not found." });

            // Check if user owns this API key
            if (apiKey.UserId != userId)
                return Forbid();

            // Validate provider access if specific providers are requested
            if (request.AllowedProviderIds != null && request.AllowedProviderIds.Any())
            {
                var userAccessibleProviders = await storageProviderService.GetUserAccessibleProvidersAsync(userId);
                var userProviderIds = userAccessibleProviders.Select(p => p.Id).ToHashSet();

                var invalidProviders = request.AllowedProviderIds.Where(pid => !userProviderIds.Contains(pid)).ToList();
                if (invalidProviders.Any())
                {
                    return Forbid();
                }
            }

            // Update API key properties
            if (!string.IsNullOrEmpty(request.Name)) apiKey.Name = request.Name;
            if (request.ExpiresAt.HasValue) apiKey.ExpiresAt = request.ExpiresAt;
            if (request.Permission != null) apiKey.Permissions = request.Permission.Value;
            if (request.AllowedProviderIds != null) apiKey.AllowedProviderIds = request.AllowedProviderIds.ToList();

            await apiKeyService.UpdateApiKeyAsync(apiKey);

            var response = new
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Key = apiKey.Key,
                ExpiresAt = apiKey.ExpiresAt,
                Permission = apiKey.Permissions.ToString(),
                IsActive = apiKey.IsActive,
                LastUsedAt = apiKey.LastUsedAt,
                ProviderCount = apiKey.AllowedProviderIds.Count
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating API key {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating the API key." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteApiKey(Guid id)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var apiKey = await apiKeyService.GetApiKeyByIdAsync(id);

            var hasAdminAccess = await authorizationService.AuthorizeAsync(User, "Admin.Access");
            if (apiKey.UserId != userId && !hasAdminAccess.Succeeded) return Forbid();

            await apiKeyService.DeleteApiKeyAsync(id);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting API key {Id}", id);
            return StatusCode(500, "Error deleting API key");
        }
    }

    [HttpPost("{id}/deactivate")]
    public async Task<ActionResult> DeactivateApiKey(Guid id)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var apiKey = await apiKeyService.GetApiKeyByIdAsync(id);

            var hasAdminAccess = await authorizationService.AuthorizeAsync(User, "Admin.Access");
            if (apiKey.UserId != userId && !hasAdminAccess.Succeeded) return Forbid();

            await apiKeyService.DeactivateApiKeyAsync(id);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deactivating API key {Id}", id);
            return StatusCode(500, "Error deactivating API key");
        }
    }

    /// <summary>
    /// Gets API Key usage activities
    /// </summary>
    /// <param name="id">API Key ID</param>
    /// <param name="startDate">Start date (optional)</param>
    /// <param name="endDate">End date (optional)</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>API Key activity logs</returns>
    [HttpGet("{id}/activity")]
    public async Task<ActionResult<AuthDTOs.ApiKeyActivityResponseDto>> GetApiKeyActivity(
        Guid id,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var apiKey = await apiKeyService.GetApiKeyByIdAsync(id);

            var hasAdminAccess = await authorizationService.AuthorizeAsync(User, "Admin.Access");
            if (apiKey.UserId != userId && !hasAdminAccess.Succeeded) return Forbid();

            var (activities, totalCount) = await auditService.GetApiKeyActivitiesAsync(
                id.ToString(), startDate, endDate, page, pageSize);

            var activityItems = activities.Select(a => new AuthDTOs.ApiKeyActivityItemDto
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                Description = a.Description,
                Method = ParseMethodFromJson(a.Data),
                Path = ParsePathFromJson(a.Data),
                StatusCode = ParseStatusCodeFromJson(a.Data)
            }).ToList();

            var response = new AuthDTOs.ApiKeyActivityResponseDto
            {
                ApiKeyId = id,
                ApiKeyName = apiKey.Name,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Activities = activityItems
            };

            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting API key activity logs for key {Id}", id);
            return StatusCode(500, "Error retrieving API key activity logs");
        }
    }

    /// <summary>
    /// Extracts Method information from JSON data
    /// </summary>
    private string ParseMethodFromJson(string jsonData)
    {
        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonData);
            return jsonDoc.RootElement.GetProperty("Request").GetProperty("Method").GetString() ?? "UNKNOWN";
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    /// <summary>
    /// Extracts Path information from JSON data
    /// </summary>
    private string ParsePathFromJson(string jsonData)
    {
        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonData);
            return jsonDoc.RootElement.GetProperty("Request").GetProperty("Path").GetString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Extracts StatusCode information from JSON data
    /// </summary>
    private int ParseStatusCodeFromJson(string jsonData)
    {
        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonData);
            return jsonDoc.RootElement.GetProperty("Response").GetProperty("StatusCode").GetInt32();
        }
        catch
        {
            return 0;
        }
    }
} 
