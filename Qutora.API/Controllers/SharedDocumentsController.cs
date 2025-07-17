using Microsoft.AspNetCore.Mvc;
using Qutora.Application.Interfaces;
using Qutora.Shared.DTOs;

namespace Qutora.API.Controllers;

/// <summary>
/// Public API controller for viewing shared documents
/// </summary>
[ApiController]
[Route("api/shared-documents")]
public class SharedDocumentsController(
    IDocumentShareService documentShareService,
    IDocumentService documentService,
    ILogger<SharedDocumentsController> logger)
    : ControllerBase
{
    private readonly IDocumentService _documentService =
        documentService ?? throw new ArgumentNullException(nameof(documentService));

    private readonly IDocumentShareService _documentShareService =
        documentShareService ?? throw new ArgumentNullException(nameof(documentShareService));

    private readonly ILogger<SharedDocumentsController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets document information by share code
    /// </summary>
    [HttpGet("{shareCode}")]
    public async Task<IActionResult> GetSharedDocument(string shareCode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(shareCode)) return BadRequest("Share code is required");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var accessInfo =
                await _documentShareService.GetShareAccessInfoAsync(shareCode, ipAddress, userAgent, cancellationToken);

            if (!accessInfo.IsValid) return BadRequest(accessInfo.ErrorMessage);

            if (accessInfo.RequiresPassword)
                return Ok(new
                {
                    RequiresPassword = true,
                    ShareCode = shareCode,
                    Message = "This document is password protected. Please provide the password to access it."
                });

            var document = await _documentService.GetByIdAsync(accessInfo.DocumentId, cancellationToken);
            if (document == null) return NotFound("Document not found");

            await _documentShareService.RecordShareViewAsync(accessInfo.ShareId, ipAddress, userAgent,
                cancellationToken);

            return Ok(new
            {
                Document = document,
                ShareInfo = new
                {
                    ShareId = accessInfo.ShareId,
                    ShareCode = shareCode,
                    AllowDownload = accessInfo.AllowDownload,
                    AllowPrint = accessInfo.AllowPrint,
                    WatermarkText = accessInfo.WatermarkText,
                    ShowWatermark = accessInfo.ShowWatermark,
                    CustomMessage = accessInfo.CustomMessage,
                    RemainingViews = accessInfo.RemainingViews
                },
                RequiresPassword = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shared document with code {ShareCode}", shareCode);
            return StatusCode(500, "An error occurred while retrieving the shared document");
        }
    }

    /// <summary>
    /// Validates password for password-protected shares
    /// </summary>
    [HttpPost("{shareCode}/validate-password")]
    public async Task<IActionResult> ValidatePassword(string shareCode,
        [FromBody] SharePasswordValidationDto validationDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(shareCode)) return BadRequest("Share code is required");

            if (validationDto == null || string.IsNullOrWhiteSpace(validationDto.Password))
                return BadRequest("Password is required");

            validationDto.ShareCode = shareCode;

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var accessInfo =
                await _documentShareService.ValidateSharePasswordAsync(validationDto, ipAddress, userAgent,
                    cancellationToken);

            if (!accessInfo.IsValid) return BadRequest(accessInfo.ErrorMessage);

            var document = await _documentService.GetByIdAsync(accessInfo.DocumentId, cancellationToken);
            if (document == null) return NotFound("Document not found");

            await _documentShareService.RecordShareViewAsync(accessInfo.ShareId, ipAddress, userAgent,
                cancellationToken);

            return Ok(new
            {
                Document = document,
                ShareInfo = new
                {
                    ShareId = accessInfo.ShareId,
                    ShareCode = shareCode,
                    AllowDownload = accessInfo.AllowDownload,
                    AllowPrint = accessInfo.AllowPrint,
                    WatermarkText = accessInfo.WatermarkText,
                    ShowWatermark = accessInfo.ShowWatermark,
                    CustomMessage = accessInfo.CustomMessage,
                    RemainingViews = accessInfo.RemainingViews
                },
                RequiresPassword = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password for shared document with code {ShareCode}", shareCode);
            return StatusCode(500, "An error occurred while validating the password");
        }
    }

    /// <summary>
    /// Gets shared document content for viewing
    /// </summary>
    [HttpGet("{shareCode}/content")]
    public async Task<IActionResult> GetSharedDocumentContent(string shareCode,
        [FromQuery] string? password = null,
        [FromQuery] string? token = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Content request received - ShareCode: {ShareCode}, Password: '{Password}', HasPassword: {HasPassword}",
                shareCode, password, !string.IsNullOrWhiteSpace(password));

            if (string.IsNullOrWhiteSpace(shareCode)) return BadRequest("Share code is required");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            ShareAccessInfoDto accessInfo;

            if (!string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation("Validating password for content access - ShareCode: {ShareCode}", shareCode);
                var validationDto = new SharePasswordValidationDto
                {
                    ShareCode = shareCode,
                    Password = password
                };
                accessInfo =
                    await _documentShareService.ValidateSharePasswordAsync(validationDto, ipAddress, userAgent,
                        cancellationToken);
                _logger.LogInformation("Password validation result - IsValid: {IsValid}, Error: {Error}",
                    accessInfo.IsValid, accessInfo.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("No password provided, getting basic access info - ShareCode: {ShareCode}",
                    shareCode);
                accessInfo =
                    await _documentShareService.GetShareAccessInfoAsync(shareCode, ipAddress, userAgent,
                        cancellationToken);
                _logger.LogInformation(
                    "Basic access info result - IsValid: {IsValid}, RequiresPassword: {RequiresPassword}, Error: {Error}",
                    accessInfo.IsValid, accessInfo.RequiresPassword, accessInfo.ErrorMessage);
            }

            if (!accessInfo.IsValid)
            {
                _logger.LogWarning("Access denied for content - ShareCode: {ShareCode}, Error: {Error}",
                    shareCode, accessInfo.ErrorMessage);
                return BadRequest(accessInfo.ErrorMessage);
            }

            if (accessInfo.RequiresPassword && string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation("Password required for content access - ShareCode: {ShareCode}", shareCode);
                return BadRequest("Password is required to access this document");
            }

            _logger.LogInformation("Content access granted - ShareCode: {ShareCode}, DocumentId: {DocumentId}",
                shareCode, accessInfo.DocumentId);

            var documentContent =
                await _documentService.GetDocumentContentAsync(accessInfo.DocumentId, cancellationToken);
            if (documentContent == null)
            {
                _logger.LogError("Document content not found - DocumentId: {DocumentId}", accessInfo.DocumentId);
                return NotFound("Document content not found");
            }

            await _documentShareService.RecordShareViewAsync(accessInfo.ShareId, ipAddress, userAgent,
                cancellationToken);

            _logger.LogInformation("Content access successful - ShareCode: {ShareCode}, FileName: {FileName}",
                shareCode, documentContent.FileName);

            return File(documentContent.Content, documentContent.ContentType, documentContent.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving content for shared document with code {ShareCode}", shareCode);
            return StatusCode(500, $"Content error: {ex.Message} | Inner: {ex.InnerException?.Message}");
        }
    }

    /// <summary>
    /// Downloads shared document
    /// </summary>
    [HttpGet("{shareCode}/download")]
    public async Task<IActionResult> DownloadSharedDocument(string shareCode, [FromQuery] string? password = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Download request received - ShareCode: {ShareCode}, Password: '{Password}', HasPassword: {HasPassword}",
                shareCode, password, !string.IsNullOrWhiteSpace(password));

            if (string.IsNullOrWhiteSpace(shareCode)) return BadRequest("Share code is required");

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            ShareAccessInfoDto accessInfo;

            if (!string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation("Validating password for download - ShareCode: {ShareCode}", shareCode);
                var validationDto = new SharePasswordValidationDto
                {
                    ShareCode = shareCode,
                    Password = password
                };
                accessInfo =
                    await _documentShareService.ValidateSharePasswordAsync(validationDto, ipAddress, userAgent,
                        cancellationToken);
                _logger.LogInformation("Password validation result - IsValid: {IsValid}, Error: {Error}",
                    accessInfo.IsValid, accessInfo.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("No password provided, getting basic access info - ShareCode: {ShareCode}",
                    shareCode);
                accessInfo =
                    await _documentShareService.GetShareAccessInfoAsync(shareCode, ipAddress, userAgent,
                        cancellationToken);
                _logger.LogInformation(
                    "Basic access info result - IsValid: {IsValid}, RequiresPassword: {RequiresPassword}, Error: {Error}",
                    accessInfo.IsValid, accessInfo.RequiresPassword, accessInfo.ErrorMessage);
            }

            if (!accessInfo.IsValid)
            {
                _logger.LogWarning("Access denied for download - ShareCode: {ShareCode}, Error: {Error}",
                    shareCode, accessInfo.ErrorMessage);
                return BadRequest(accessInfo.ErrorMessage);
            }

            if (!accessInfo.AllowDownload)
            {
                _logger.LogWarning("Download not allowed - ShareCode: {ShareCode}", shareCode);
                return StatusCode(403, new { error = "Download is not allowed for this share" });
            }

            _logger.LogInformation("Download access granted - ShareCode: {ShareCode}, DocumentId: {DocumentId}",
                shareCode, accessInfo.DocumentId);

            var documentContent =
                await _documentService.GetDocumentContentAsync(accessInfo.DocumentId, cancellationToken);
            if (documentContent == null)
            {
                _logger.LogError("Document content not found - DocumentId: {DocumentId}", accessInfo.DocumentId);
                return NotFound("Document content not found");
            }

            await _documentShareService.RecordShareViewAsync(accessInfo.ShareId, ipAddress, userAgent,
                cancellationToken);

            _logger.LogInformation("Download successful - ShareCode: {ShareCode}, FileName: {FileName}, Size: {Size}",
                shareCode, documentContent.FileName, documentContent.Content.Length);

            return File(documentContent.Content, documentContent.ContentType, documentContent.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading shared document with code {ShareCode}", shareCode);
            return StatusCode(500, $"Download error: {ex.Message} | Inner: {ex.InnerException?.Message}");
        }
    }


}
