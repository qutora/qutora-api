using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Infrastructure.Interfaces;
using Qutora.Shared.DTOs.Email;
using Qutora.Shared.DTOs.Common;

namespace Qutora.API.Controllers;

/// <summary>
/// Controller for email settings and operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "SystemSettings")]
public class EmailController(IEmailService emailService, ILogger<EmailController> logger) : ControllerBase
{
    /// <summary>
    /// Get current email settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<EmailSettingsDto>> GetSettings()
    {
        try
        {
            var settings = await emailService.GetSettingsAsync();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting email settings");
            return StatusCode(500, "An error occurred while getting email settings");
        }
    }

    /// <summary>
    /// Update email settings
    /// </summary>
    [HttpPut("settings")]
    public async Task<ActionResult<EmailSettingsDto>> UpdateSettings([FromBody] UpdateEmailSettingsDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var settings = await emailService.UpdateSettingsAsync(dto);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating email settings");
            return StatusCode(500, "An error occurred while updating email settings");
        }
    }

    /// <summary>
    /// Send test email
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult<MessageResponseDto>> SendTestEmail([FromBody] SendTestEmailDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await emailService.SendTestEmailAsync(dto.ToEmail);
            
            if (success)
                return Ok(MessageResponseDto.SuccessResponse("Test email sent successfully"));
            else
                return BadRequest(MessageResponseDto.ErrorResponse("Failed to send test email"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending test email");
            return StatusCode(500, "An error occurred while sending test email");
        }
    }

    /// <summary>
    /// Send test template email
    /// </summary>
    [HttpPost("test-template")]
    public async Task<ActionResult<MessageResponseDto>> SendTestTemplateEmail([FromBody] SendTestTemplateEmailDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await emailService.SendTestTemplateEmailAsync(dto.TemplateId, dto.ToEmail);
            
            if (success)
                return Ok(MessageResponseDto.SuccessResponse("Test template email sent successfully"));
            else
                return BadRequest(MessageResponseDto.ErrorResponse("Test email gönderilemedi. Lütfen ayarlarınızı kontrol edin."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending test template email with template {TemplateId}", dto.TemplateId);
            return StatusCode(500, "An error occurred while sending test template email");
        }
    }
} 