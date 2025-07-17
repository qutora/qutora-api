using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapsterMapper;
using Qutora.Application.Interfaces;
using Qutora.Shared.DTOs;

namespace Qutora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetadataController(
    IMetadataService metadataService,
    ILogger<MetadataController> logger,
    IMapper mapper)
    : ControllerBase
{
    private readonly IMapper _mapper = mapper;

    [HttpGet("document/{documentId}")]
    [Authorize(Policy = "Metadata.Read")]
    public async Task<IActionResult> GetDocumentMetadata(Guid documentId)
    {
        try
        {
            var metadata = await metadataService.GetByDocumentIdAsync(documentId);

            if (metadata == null) return NotFound("No metadata found for document");

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metadata for document {DocumentId}", documentId);
            return StatusCode(500, "An error occurred while retrieving metadata");
        }
    }

    [HttpPost("document/{documentId}")]
    [Authorize(Policy = "Metadata.Create")]
    public async Task<IActionResult> CreateMetadata(Guid documentId,
        [FromBody] CreateUpdateMetadataDto createMetadataDto)
    {
        try
        {
            if (!string.IsNullOrEmpty(createMetadataDto.SchemaName) && createMetadataDto.Values?.Count > 0)
            {
                var validationErrors = await metadataService.ValidateMetadataAsync(createMetadataDto.SchemaName, createMetadataDto.Values);
                if (validationErrors.Count > 0)
                {
                    var errorMessages = string.Join("; ", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                    return BadRequest($"Metadata validation failed: {errorMessages}");
                }
            }

            var metadata = await metadataService.CreateAsync(documentId, createMetadataDto);
            return CreatedAtAction(nameof(GetDocumentMetadata), new { documentId }, metadata);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating metadata for document {DocumentId}", documentId);
            return StatusCode(500, "An error occurred while creating metadata");
        }
    }

    [HttpPut("document/{documentId}")]
    [Authorize(Policy = "Metadata.Update")]
    public async Task<IActionResult> UpdateMetadata(Guid documentId,
        [FromBody] CreateUpdateMetadataDto updateMetadataDto)
    {
        try
        {
            if (!string.IsNullOrEmpty(updateMetadataDto.SchemaName) && updateMetadataDto.Values?.Count > 0)
            {
                var validationErrors = await metadataService.ValidateMetadataAsync(updateMetadataDto.SchemaName, updateMetadataDto.Values);
                if (validationErrors.Count > 0)
                {
                    var errorMessages = string.Join("; ", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                    return BadRequest($"Metadata validation failed: {errorMessages}");
                }
            }

            var metadata = await metadataService.UpdateAsync(documentId, updateMetadataDto);
            return Ok(metadata);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating metadata for document {DocumentId}", documentId);
            return StatusCode(500, "An error occurred while updating metadata");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Metadata.Delete")]
    public async Task<IActionResult> DeleteMetadata(Guid id)
    {
        try
        {
            var result = await metadataService.DeleteAsync(id);

            if (!result) return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting metadata with ID {MetadataId}", id);
            return StatusCode(500, "An error occurred while deleting metadata");
        }
    }

    [HttpGet("search")]
    [Authorize(Policy = "Metadata.Read")]
    public async Task<ActionResult<PagedDto<MetadataDto>>> SearchByMetadata([FromQuery] string searchCriteriaJson, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var searchCriteria =
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(searchCriteriaJson);

            if (searchCriteria == null || searchCriteria.Count == 0) return BadRequest("Search criteria is required");

            var results = await metadataService.SearchAsync(searchCriteria, page, pageSize);
            return Ok(results);
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest("Invalid search criteria format");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching by metadata");
            return StatusCode(500, "An error occurred while searching by metadata");
        }
    }

    [HttpGet("tags")]
    [Authorize(Policy = "Metadata.Read")]
    public async Task<ActionResult<PagedDto<MetadataDto>>> GetByTags([FromQuery] string tags, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var tagArray = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (tagArray.Length == 0) return BadRequest("At least one tag is required");

            var results = await metadataService.GetByTagsAsync(tagArray, page, pageSize);
            return Ok(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting metadata by tags");
            return StatusCode(500, "An error occurred while getting metadata by tags");
        }
    }

    [HttpPost("validate")]
    [Authorize(Policy = "Metadata.Read")]
    public async Task<IActionResult> ValidateMetadata([FromQuery] string schemaName,
        [FromBody] Dictionary<string, object> metadata)
    {
        try
        {
            var validationResult = await metadataService.ValidateMetadataAsync(schemaName, metadata);

            if (validationResult.Count == 0) return Ok(new { IsValid = true });

            return BadRequest(new { IsValid = false, Errors = validationResult });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating metadata");
            return StatusCode(500, "An error occurred while validating metadata");
        }
    }
}
