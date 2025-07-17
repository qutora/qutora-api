using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Shared.DTOs;
using System.Security.Claims;
using Qutora.Application.Interfaces;

namespace Qutora.API.Controllers;

/// <summary>
/// API controller for document sharing operations
/// </summary>
[ApiController]
[Route("api/document-shares")]
[Authorize]
public class DocumentSharesController(
    IDocumentShareService documentShareService,
    ILogger<DocumentSharesController> logger)
    : ControllerBase
{
    private readonly IDocumentShareService _documentShareService =
        documentShareService ?? throw new ArgumentNullException(nameof(documentShareService));

    private readonly ILogger<DocumentSharesController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Creates a new document share
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Document.Share")]
    public async Task<IActionResult> CreateShare([FromBody] DocumentShareCreateDto shareDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (shareDto == null) return BadRequest("Share data is required");

            var share = await _documentShareService.CreateShareAsync(shareDto, cancellationToken);
            return CreatedAtAction(nameof(GetShare), new { id = share.Id }, share);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Document not found for sharing: {DocumentId}", shareDto.DocumentId);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during share creation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document share for document {DocumentId}", shareDto.DocumentId);
            return StatusCode(500, "An error occurred while creating the document share");
        }
    }

    /// <summary>
    /// Gets document share by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "Document.Share")]
    public async Task<IActionResult> GetShare(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var share = await _documentShareService.GetByIdAsync(id, cancellationToken);
            if (share == null) return NotFound();

            return Ok(share);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document share with ID {ShareId}", id);
            return StatusCode(500, "An error occurred while retrieving the document share");
        }
    }

    /// <summary>
    /// Updates document share
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "Document.Share")]
    public async Task<IActionResult> UpdateShare(Guid id, [FromBody] DocumentShareCreateDto updateDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (updateDto == null) return BadRequest("Update data is required");

            var share = await _documentShareService.UpdateShareAsync(id, updateDto, cancellationToken);
            if (share == null) return NotFound();

            return Ok(share);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document share {ShareId}", id);
            return StatusCode(500, "An error occurred while updating the document share");
        }
    }

    /// <summary>
    /// Gets shares for a document
    /// </summary>
    [HttpGet("document/{documentId}")]
    [Authorize(Policy = "Document.Share")]
    public async Task<IActionResult> GetSharesByDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var shares = await _documentShareService.GetByDocumentIdAsync(documentId, cancellationToken);
            return Ok(shares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shares for document {DocumentId}", documentId);
            return StatusCode(500, "An error occurred while retrieving document shares");
        }
    }

    /// <summary>
    /// Gets current user's shares
    /// </summary>
    [HttpGet("my-shares")]
    [Authorize(Policy = "Document.Share")]
    public async Task<IActionResult> GetMyShares([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("User not found");

            var shares = await _documentShareService.GetUserSharesPagedAsync(currentUserId, page, pageSize, searchTerm, cancellationToken);
            return Ok(shares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user document shares");
            return StatusCode(500, "An error occurred while retrieving document shares");
        }
    }

    /// <summary>
    /// Deletes document share
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "Document.Share")]
    public async Task<IActionResult> DeleteShare(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _documentShareService.DeleteShareAsync(id, cancellationToken);
            if (!result) return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document share {ShareId}", id);
            return StatusCode(500, "An error occurred while deleting the document share");
        }
    }

    /// <summary>
    /// Toggles document share status (active/inactive)
    /// </summary>
    [HttpPost("{id}/toggle-status")]
    [Authorize(Policy = "Document.Share")]
    public async Task<IActionResult> ToggleShareStatus(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _documentShareService.ToggleShareStatusAsync(id, cancellationToken);
            if (!result) return NotFound();

            return Ok(new { message = "Share status successfully changed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling document share status {ShareId}", id);
            return StatusCode(500, "An error occurred while toggling the document share status");
        }
    }

    /// <summary>
    /// Gets user share view trend data for charts
    /// </summary>
    [HttpGet("view-trend")]
    [Authorize(Policy = "Document.Share")]
    public async Task<IActionResult> GetViewTrend([FromQuery] int monthCount = 6, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized("User not found");

            var trendData = await _documentShareService.GetUserShareViewTrendAsync(currentUserId, monthCount, cancellationToken);
            return Ok(trendData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving share view trend data");
            return StatusCode(500, "An error occurred while retrieving trend data");
        }
    }
}
