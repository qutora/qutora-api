using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapsterMapper;
using Qutora.Application.Interfaces;
using Qutora.Shared.DTOs;

namespace Qutora.API.Controllers;

[ApiController]
[Route("api/metadata/schemas")]
[Authorize]
public class MetadataSchemaController(
    IMetadataSchemaService metadataSchemaService,
    ILogger<MetadataSchemaController> logger,
    IMapper mapper)
    : ControllerBase
{
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    [Authorize(Policy = "MetadataSchema.Read")]
    public async Task<ActionResult<PagedDto<MetadataSchemaDto>>> GetAllSchemas([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string query = "")
    {
        try
        {
            var pagedSchemas = await metadataSchemaService.GetAllAsync(page, pageSize, query);
            return Ok(pagedSchemas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metadata schemas");
            return StatusCode(500, "An error occurred while retrieving metadata schemas");
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "MetadataSchema.Read")]
    public async Task<IActionResult> GetSchemaById(Guid id)
    {
        try
        {
            var schema = await metadataSchemaService.GetByIdAsync(id);

            if (schema == null) return NotFound();

            return Ok(schema);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metadata schema with ID {SchemaId}", id);
            return StatusCode(500, "An error occurred while retrieving metadata schema");
        }
    }

    [HttpGet("name/{name}")]
    [Authorize(Policy = "MetadataSchema.Read")]
    public async Task<IActionResult> GetSchemaByName(string name)
    {
        try
        {
            var schema = await metadataSchemaService.GetByNameAsync(name);

            if (schema == null) return NotFound();

            return Ok(schema);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metadata schema with name {SchemaName}", name);
            return StatusCode(500, "An error occurred while retrieving metadata schema");
        }
    }

    [HttpPost]
    [Authorize(Policy = "MetadataSchema.Create")]
    public async Task<IActionResult> CreateSchema([FromBody] CreateUpdateMetadataSchemaDto createSchemaDto)
    {
        try
        {
            // Debug: İncoming request'i log'la
            logger.LogInformation("Creating metadata schema: {SchemaName} with {FieldCount} fields", 
                createSchemaDto.Name, createSchemaDto.Fields?.Count ?? 0);
            
            if (createSchemaDto.Fields != null)
            {
                foreach (var field in createSchemaDto.Fields.Select((f, i) => new { Field = f, Index = i }))
                {
                    logger.LogInformation("Field {Index}: Name={Name}, Type={Type}, OptionCount={OptionCount}", 
                        field.Index, field.Field.Name, field.Field.Type, field.Field.OptionItems?.Count ?? 0);
                    
                    if (field.Field.OptionItems != null)
                    {
                        foreach (var option in field.Field.OptionItems.Select((o, i) => new { Option = o, Index = i }))
                        {
                            logger.LogInformation("  Option {OptionIndex}: Label={Label}, Value={Value}", 
                                option.Index, option.Option.Label, option.Option.Value);
                        }
                    }
                }
            }
            
            var schema = await metadataSchemaService.CreateAsync(createSchemaDto);
            return CreatedAtAction(nameof(GetSchemaById), new { id = schema.Id }, schema);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error creating metadata schema");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation creating metadata schema");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating metadata schema");
            return StatusCode(500, new { message = "An error occurred while creating metadata schema" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "MetadataSchema.Update")]
    public async Task<IActionResult> UpdateSchema(Guid id, [FromBody] CreateUpdateMetadataSchemaDto updateSchemaDto)
    {
        try
        {
            var schema = await metadataSchemaService.UpdateAsync(id, updateSchemaDto);
            return Ok(schema);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error updating metadata schema");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation updating metadata schema");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating metadata schema with ID {SchemaId}", id);
            return StatusCode(500, new { message = "An error occurred while updating metadata schema" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "MetadataSchema.Delete")]
    public async Task<IActionResult> DeleteSchema(Guid id)
    {
        try
        {
            var result = await metadataSchemaService.DeleteAsync(id);

            if (!result) return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting metadata schema with ID {SchemaId}", id);
            return StatusCode(500, "An error occurred while deleting metadata schema");
        }
    }

    [HttpGet("filetype/{fileType}")]
    [Authorize(Policy = "MetadataSchema.Read")]
    public async Task<IActionResult> GetSchemasByFileType(string fileType)
    {
        try
        {
            var schemas = await metadataSchemaService.GetByFileTypeAsync(fileType);
            return Ok(schemas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metadata schemas for file type {FileType}", fileType);
            return StatusCode(500, "An error occurred while retrieving metadata schemas");
        }
    }

    [HttpGet("category/{categoryId}")]
    [Authorize(Policy = "MetadataSchema.Read")]
    public async Task<IActionResult> GetSchemasByCategory(Guid categoryId)
    {
        try
        {
            var schemas = await metadataSchemaService.GetByCategoryIdAsync(categoryId);
            return Ok(schemas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metadata schemas for category {CategoryId}", categoryId);
            return StatusCode(500, "An error occurred while retrieving metadata schemas");
        }
    }

    [HttpGet("all")]
    [Authorize(Policy = "MetadataSchema.Read")]
    public async Task<ActionResult<IEnumerable<MetadataSchemaDto>>> GetAll()
    {
        try
        {
            var schemas = await metadataSchemaService.GetAllSchemasAsync();
            var simplifiedSchemas = schemas.Select(s => new MetadataSchemaDto
            {
                Id = s.Id,
                Name = s.Name,
                CategoryId = s.CategoryId
            });

            return Ok(simplifiedSchemas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving metadata schemas");
            return StatusCode(500,
                new { message = "An error occurred while retrieving metadata schemas", error = ex.Message });
        }
    }
}
