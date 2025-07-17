using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Application.Interfaces;
using Qutora.Shared.DTOs.Email;
using Qutora.Shared.DTOs.Common;

namespace Qutora.API.Controllers;

/// <summary>
/// Controller for email template management
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "SystemSettings")]
public class EmailTemplatesController(IEmailService emailService, ILogger<EmailTemplatesController> logger) : ControllerBase
{
    /// <summary>
    /// Get all email templates
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EmailTemplateDto>>> GetTemplates()
    {
        try
        {
            var templates = await emailService.GetTemplatesAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting email templates");
            return StatusCode(500, "An error occurred while getting email templates");
        }
    }

    /// <summary>
    /// Get email template by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmailTemplateDto>> GetTemplate(Guid id)
    {
        try
        {
            var template = await emailService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound();

            return Ok(template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting email template {Id}", id);
            return StatusCode(500, "An error occurred while getting email template");
        }
    }

    /// <summary>
    /// Create new email template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmailTemplateDto>> CreateTemplate([FromBody] CreateEmailTemplateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = await emailService.CreateTemplateAsync(dto);
            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid template data: {Message}", ex.Message);
            return BadRequest(MessageResponseDto.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating email template");
            return StatusCode(500, "An error occurred while creating email template");
        }
    }

    /// <summary>
    /// Update email template
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmailTemplateDto>> UpdateTemplate(Guid id, [FromBody] UpdateEmailTemplateDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var template = await emailService.UpdateTemplateAsync(id, dto);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid template data: {Message}", ex.Message);
            return BadRequest(MessageResponseDto.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
            return BadRequest(MessageResponseDto.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating email template {Id}", id);
            return StatusCode(500, "An error occurred while updating email template");
        }
    }

    /// <summary>
    /// Delete email template
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<MessageResponseDto>> DeleteTemplate(Guid id)
    {
        try
        {
            var success = await emailService.DeleteTemplateAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation: {Message}", ex.Message);
            return BadRequest(MessageResponseDto.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting email template {Id}", id);
            return StatusCode(500, "An error occurred while deleting email template");
        }
    }
} 