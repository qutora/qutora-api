using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Approval;
using System.Security.Claims;
using Qutora.Application.Interfaces;

namespace Qutora.API.Controllers;

/// <summary>
/// Document sharing approval system API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly IApprovalPolicyService _approvalPolicyService;
    private readonly IApprovalSettingsService _approvalSettingsService;
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(
        IApprovalService approvalService,
        IApprovalPolicyService approvalPolicyService,
        IApprovalSettingsService approvalSettingsService,
        ILogger<ApprovalController> logger)
    {
        _approvalService = approvalService;
        _approvalPolicyService = approvalPolicyService;
        _approvalSettingsService = approvalSettingsService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    #region Global Settings Management

    /// <summary>
    /// Get current approval settings
    /// </summary>
    [HttpGet("settings")]
    [Authorize(Policy = "ApprovalSettings.Read")]
    public async Task<ActionResult<ApprovalSettingsDto>> GetApprovalSettings()
    {
        var settings = await _approvalSettingsService.GetCurrentSettingsAsync();
        return Ok(settings);
    }

    /// <summary>
    /// Update approval settings
    /// </summary>
    [HttpPut("settings")]
    [Authorize(Policy = "ApprovalSettings.Update")]
    public async Task<ActionResult<ApprovalSettingsDto>> UpdateApprovalSettings(
        [FromBody] UpdateApprovalSettingsDto dto)
    {
        var settings = await _approvalSettingsService.UpdateSettingsAsync(dto);
        _logger.LogInformation("Approval settings updated by user {UserId}", GetCurrentUserId());
        return Ok(settings);
    }

    /// <summary>
    /// Enable global approval
    /// </summary>
    [HttpPost("settings/enable-global")]
    [Authorize(Policy = "ApprovalSettings.Update")]
    public async Task<IActionResult> EnableGlobalApproval([FromBody] EnableGlobalApprovalRequestDto request)
    {
        await _approvalSettingsService.EnableGlobalApprovalAsync(request.Reason, GetCurrentUserId());
        _logger.LogInformation("Global approval enabled by user {UserId}", GetCurrentUserId());
        return Ok(new { Message = "Global approval enabled successfully" });
    }

    /// <summary>
    /// Disable global approval
    /// </summary>
    [HttpPost("settings/disable-global")]
    [Authorize(Policy = "ApprovalSettings.Update")]
    public async Task<IActionResult> DisableGlobalApproval()
    {
        await _approvalSettingsService.DisableGlobalApprovalAsync(GetCurrentUserId());
        _logger.LogInformation("Global approval disabled by user {UserId}", GetCurrentUserId());
        return Ok(new { Message = "Global approval disabled successfully" });
    }

    /// <summary>
    /// Reset approval settings to default
    /// </summary>
    [HttpGet("settings/is-global-enabled")]
    [Authorize(Policy = "ApprovalSettings.Read")]
    public async Task<ActionResult<bool>> IsGlobalApprovalEnabled()
    {
        var isEnabled = await _approvalSettingsService.IsGlobalApprovalEnabledAsync();
        return Ok(isEnabled);
    }

    #endregion

    #region Policy Management

    /// <summary>
    /// Create new approval policy
    /// </summary>
    [HttpPost("policies")]
    [Authorize(Policy = "ApprovalPolicy.Create")]
    public async Task<ActionResult<ApprovalPolicyDto>> CreatePolicy([FromBody] CreateApprovalPolicyDto dto,
        CancellationToken cancellationToken = default)
    {
        dto.CreatedByUserId = GetCurrentUserId();
        var policy = await _approvalPolicyService.CreateAsync(dto, cancellationToken);

        _logger.LogInformation("Approval policy created: {PolicyId} by user {UserId}", policy.Id, GetCurrentUserId());
        return CreatedAtAction(nameof(GetPolicyById), new { id = policy.Id }, policy);
    }

    /// <summary>
    /// Get paged list of approval policies
    /// </summary>
    [HttpGet("policies")]
    [Authorize(Policy = "ApprovalPolicy.Read")]
    public async Task<ActionResult<PagedDto<ApprovalPolicyDto>>> GetPolicies([FromQuery] ApprovalPolicyQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var policies = await _approvalPolicyService.GetPagedAsync(query, cancellationToken);
        return Ok(policies);
    }

    /// <summary>
    /// Get approval policy by ID
    /// </summary>
    [HttpGet("policies/{id}")]
    [Authorize(Policy = "ApprovalPolicy.Read")]
    public async Task<ActionResult<ApprovalPolicyDto>> GetPolicyById(Guid id,
        CancellationToken cancellationToken = default)
    {
        var policy = await _approvalPolicyService.GetByIdAsync(id, cancellationToken);
        if (policy == null)
            return NotFound($"Approval policy with ID {id} not found.");

        return Ok(policy);
    }

    /// <summary>
    /// Update approval policy
    /// </summary>
    [HttpPut("policies/{id}")]
    [Authorize(Policy = "ApprovalPolicy.Update")]
    public async Task<ActionResult<ApprovalPolicyDto>> UpdatePolicy(Guid id, [FromBody] UpdateApprovalPolicyDto dto,
        CancellationToken cancellationToken = default)
    {
        var policy = await _approvalPolicyService.UpdateAsync(id, dto, cancellationToken);
        if (policy == null)
            return NotFound($"Approval policy with ID {id} not found.");

        _logger.LogInformation("Approval policy updated: {PolicyId} by user {UserId}", id, GetCurrentUserId());
        return Ok(policy);
    }

    /// <summary>
    /// Delete approval policy
    /// </summary>
    [HttpDelete("policies/{id}")]
    [Authorize(Policy = "ApprovalPolicy.Delete")]
    public async Task<IActionResult> DeletePolicy(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _approvalPolicyService.DeleteAsync(id, cancellationToken);
        if (!success)
            return NotFound($"Approval policy with ID {id} not found.");

        _logger.LogInformation("Approval policy deleted: {PolicyId} by user {UserId}", id, GetCurrentUserId());
        return NoContent();
    }

    /// <summary>
    /// Toggle approval policy status
    /// </summary>
    [HttpPost("policies/{id}/toggle")]
    [Authorize(Policy = "ApprovalPolicy.Update")]
    public async Task<ActionResult<ApprovalPolicyDto>> TogglePolicyStatus(Guid id,
        CancellationToken cancellationToken = default)
    {
        var policy = await _approvalPolicyService.TogglePolicyStatusAsync(id, cancellationToken);
        if (policy == null)
            return NotFound($"Approval policy with ID {id} not found.");

        _logger.LogInformation("Approval policy status toggled: {PolicyId} by user {UserId}", id, GetCurrentUserId());
        return Ok(policy);
    }

    #endregion

    #region Approval Requests

    /// <summary>
    /// Get pending approval requests
    /// </summary>
    [HttpGet("requests/pending")]
    [Authorize(Policy = "Approval.Read")]
    public async Task<ActionResult<PagedDto<ShareApprovalRequestDto>>> GetPendingApprovals(
        [FromQuery] ApprovalRequestQueryDto query, CancellationToken cancellationToken = default)
    {
        var requests = await _approvalService.GetPendingApprovalsAsync(query, cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Get my approval requests
    /// </summary>
    [HttpGet("requests/my-requests")]
    [Authorize(Policy = "Approval.Read")]
    public async Task<ActionResult<PagedDto<ShareApprovalRequestDto>>> GetMyApprovalRequests(
        [FromQuery] ApprovalRequestQueryDto query, CancellationToken cancellationToken = default)
    {
        query.RequesterUserId = GetCurrentUserId();
        var requests = await _approvalService.GetMyApprovalRequestsAsync(query, cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Get approval request by ID
    /// </summary>
    [HttpGet("requests/{id}")]
    [Authorize(Policy = "Approval.Read")]
    public async Task<ActionResult<ShareApprovalRequestDto>> GetRequestById(Guid id,
        CancellationToken cancellationToken = default)
    {
        var request = await _approvalService.GetRequestByIdAsync(id, cancellationToken);
        if (request == null)
            return NotFound($"Approval request with ID {id} not found.");

        return Ok(request);
    }

    /// <summary>
    /// Process approval (approve or reject)
    /// </summary>
    [HttpPost("requests/{id}/process")]
    [Authorize(Policy = "Approval.Process")]
    public async Task<ActionResult<ApprovalResultDto>> ProcessApproval(Guid id,
        [FromBody] ProcessApprovalRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = await _approvalService.ProcessApprovalAsync(id, request.Decision, request.Comment,
            GetCurrentUserId(), cancellationToken);

        _logger.LogInformation("Approval request {RequestId} processed with decision {Decision} by user {UserId}",
            id, request.Decision, GetCurrentUserId());

        return Ok(result);
    }

    /// <summary>
    /// Get approval history for a request
    /// </summary>
    [HttpGet("requests/{id}/history")]
    [Authorize(Policy = "Approval.Read")]
    public async Task<ActionResult<List<ApprovalHistoryDto>>> GetApprovalHistory(Guid id,
        CancellationToken cancellationToken = default)
    {
        var history = await _approvalService.GetApprovalHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    #endregion

    #region Dashboard & Statistics

    /// <summary>
    /// Get approval statistics for dashboard
    /// </summary>
    [HttpGet("dashboard/statistics")]
    [Authorize(Policy = "Approval.Manage")]
    public async Task<ActionResult<ApprovalStatisticsDto>> GetApprovalStatistics(
        CancellationToken cancellationToken = default)
    {
        var statistics = await _approvalService.GetStatisticsAsync(null, null, cancellationToken);
        return Ok(statistics);
    }

    /// <summary>
    /// Process expired approval requests
    /// </summary>
    [HttpPost("maintenance/process-expired")]
    [Authorize(Policy = "Approval.Manage")]
    public async Task<IActionResult> ProcessExpiredRequests(CancellationToken cancellationToken = default)
    {
        await _approvalService.ProcessExpiredRequestsAsync(cancellationToken);
        _logger.LogInformation("Expired approval requests processed successfully");
        return Ok(new { Message = "Expired approval requests processed successfully", ProcessedAt = DateTime.UtcNow });
    }

    #endregion
}

 
