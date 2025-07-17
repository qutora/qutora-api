using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Shared.DTOs;
using System.Security.Claims;
using System.Text.Json;
using Qutora.Application.Interfaces;
using Qutora.Shared.DTOs.Common;

namespace Qutora.API.Controllers;

[ApiController]
[Route("api/storage/providers")]
[Authorize]
public class StorageProviderController(
    IStorageProviderService storageProviderService,
    IFileStorageService fileStorageService,
    ILogger<StorageProviderController> logger,
    IAuthorizationService authorizationService)
    : ControllerBase
{
    /// <summary>
    /// Gets all storage providers (Admin only - includes inactive providers)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "StorageProvider.Admin")]
    public async Task<ActionResult<IEnumerable<StorageProviderDto>>> GetAll()
    {
        try
        {
            var dtos = await storageProviderService.GetAllAsync();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all storage providers");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets only active storage providers (for regular users - filtered by user permissions)
    /// </summary>
    [HttpGet("active")]
    [Authorize(Policy = "StorageProvider.Read")]
    public async Task<ActionResult<IEnumerable<StorageProviderDto>>> GetAllActive()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            // Return all active providers for admins
            var authResult = await authorizationService.AuthorizeAsync(User, "StorageProvider.Admin");
            if (authResult.Succeeded)
            {
                var allActiveDtos = await storageProviderService.GetAllActiveAsync();
                return Ok(allActiveDtos);
            }

            // Return only providers they have permission for for regular users
            var userAccessibleDtos = await storageProviderService.GetUserAccessibleProvidersAsync(userId);
            return Ok(userAccessibleDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active storage providers for user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a storage provider by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "StorageProvider.Read")]
    public async Task<ActionResult<StorageProviderDto>> GetById(Guid id)
    {
        try
        {
            var dto = await storageProviderService.GetByIdAsync(id);
            if (dto == null) return NotFound();

            return Ok(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage provider with ID {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all available provider IDs
    /// </summary>
    [HttpGet("available")]
    [Authorize(Policy = "StorageProvider.Read")]
    public async Task<ActionResult<IEnumerable<string>>> GetAvailableProviders()
    {
        try
        {
            var providerIds = await fileStorageService.GetAvailableProvidersAsync();

            var providersWithDetails = new List<object>();

            foreach (var providerId in providerIds)
                if (Guid.TryParse(providerId, out var id))
                {
                    var provider = await storageProviderService.GetByIdAsync(id);
                    if (provider != null)
                        providersWithDetails.Add(new
                        {
                            provider.Id,
                            provider.Name,
                            provider.ProviderType,
                            provider.Description,
                            provider.IsDefault
                        });
                }

            return Ok(providersWithDetails);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving storage providers");
            return StatusCode(500, "An error occurred while retrieving storage providers");
        }
    }

    /// <summary>
    /// Gets all available storage provider types in the system
    /// </summary>
    [HttpGet("types")]
    [Authorize(Policy = "StorageProvider.Read")]
    public ActionResult<IEnumerable<string>> GetAvailableProviderTypes()
    {
        try
        {
            return Ok(storageProviderService.GetAvailableProviderTypes());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving available provider types");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Adds a new storage provider
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "StorageProvider.Create")]
    public async Task<ActionResult<StorageProviderDto>> Create([FromBody] StorageProviderCreateDto dto)
    {
        try
        {
            var createdProvider = await storageProviderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdProvider.Id }, createdProvider);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while creating storage provider");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating storage provider");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates a storage provider
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "StorageProvider.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] StorageProviderUpdateDto dto)
    {
        try
        {
            var success = await storageProviderService.UpdateAsync(id, dto);
            if (!success) return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while updating storage provider with ID {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating storage provider with ID {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Activates or deactivates a provider
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Policy = "StorageProvider.Update")]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] bool isActive)
    {
        try
        {
            var success = await storageProviderService.ToggleStatusAsync(id, isActive);
            if (!success) return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while toggling status for provider with ID {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling status for storage provider with ID {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Sets a provider as the default
    /// </summary>
    [HttpPatch("{id}/default")]
    [Authorize(Policy = "StorageProvider.Update")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        try
        {
            var success = await storageProviderService.SetAsDefaultAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while setting default provider with ID {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting default provider with ID {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a storage provider
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "StorageProvider.Delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var success = await storageProviderService.DeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while deleting storage provider with ID {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting storage provider with ID {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Tests connection to a storage provider configuration
    /// </summary>
    [HttpPost("test-connection")]
    [Authorize(Policy = "StorageProvider.Create")]
    public async Task<IActionResult> TestConnection([FromBody] StorageProviderTestDto dto)
    {
        try
        {
            var result = await storageProviderService.TestConnectionAsync(dto);

            return Ok(new { success = result.success, message = result.message });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid configuration: {message}", ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connection test error");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Tests connection to an existing provider by ID
    /// </summary>
    [HttpPost("test-connection/{id}")]
    [Authorize(Policy = "StorageProvider.Read")]
    public async Task<IActionResult> TestConnectionById(Guid id)
    {
        try
        {
            var provider = await storageProviderService.GetByIdAsync(id);
            if (provider == null) return NotFound();

            var testDto = new StorageProviderTestDto
            {
                Id = id,
                ProviderType = provider.ProviderType,
                ConfigJson = provider.ConfigJson
            };

            var result = await storageProviderService.TestConnectionAsync(testDto);

            return Ok(new { success = result.success, message = result.message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connection test error for provider ID {id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the configuration schema for a specific provider type
    /// </summary>
    [HttpGet("config-schema/{providerType}")]
    [Authorize(Policy = "StorageProvider.Read")]
    public ActionResult<Dictionary<string, object>> GetProviderConfigSchema(string providerType)
    {
        try
        {
            var availableTypes = storageProviderService.GetAvailableProviderTypes();
            if (!availableTypes.Contains(providerType, StringComparer.OrdinalIgnoreCase))
                return NotFound($"Provider type '{providerType}' not found");

            var schemaJson = storageProviderService.GetConfigurationSchema(providerType);
            var schema = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson);
            return Ok(schema);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Unsupported provider type: {ProviderType}", providerType);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting config schema for provider type {ProviderType}", providerType);
            return StatusCode(500, "Server error");
        }
    }

    /// <summary>
    /// Gets configuration schemas for all provider types
    /// </summary>
    [HttpGet("config-schemas")]
    [Authorize(Policy = "StorageProvider.Read")]
    public ActionResult<Dictionary<string, Dictionary<string, object>>> GetAllProviderConfigSchemas()
    {
        try
        {
            var result = new Dictionary<string, Dictionary<string, object>>();
            var availableTypes = storageProviderService.GetAvailableProviderTypes();

            foreach (var type in availableTypes)
                try
                {
                    var schemaJson = storageProviderService.GetConfigurationSchema(type);
                    var schema = JsonSerializer.Deserialize<Dictionary<string, object>>(schemaJson);
                    if (schema != null) result[type] = schema;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not get schema for provider type {ProviderType}", type);
                }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting all config schemas");
            return StatusCode(500, "Server error");
        }
    }

    /// <summary>
    /// Gets the capabilities of a storage provider
    /// </summary>
    [HttpGet("{id}/capabilities")]
    [Authorize(Policy = "StorageProvider.Read")]
    public async Task<ActionResult<StorageProviderCapabilitiesDto>> GetProviderCapabilities(Guid id)
    {
        try
        {
            var provider = await storageProviderService.GetByIdAsync(id);
            if (provider == null) return NotFound();

            var capabilities = await fileStorageService.GetProviderCapabilitiesAsync(id.ToString());
            if (capabilities == null) return StatusCode(500, "Could not detect provider capabilities");

            return Ok(capabilities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting capabilities for provider {Id}", id);
            return StatusCode(500, "Server error");
        }
    }

    /// <summary>
    /// Gets storage providers accessible by the current user
    /// </summary>
    [HttpGet("user-accessible")]
    [Authorize(Policy = "StorageProvider.Read")]
    public async Task<ActionResult<IEnumerable<StorageProviderDto>>> GetUserAccessibleProviders()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var userBuckets = await storageProviderService.GetUserAccessibleProvidersAsync(userId);
            return Ok(userBuckets);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user accessible storage providers");
            return StatusCode(500, "Internal server error");
        }
    }
}
