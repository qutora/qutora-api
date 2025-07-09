using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Application.Interfaces;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Common;

namespace Qutora.API.Controllers;

/// <summary>
/// Audit log management controller for internal audit and internal control access
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AuditController(
    IAuditService auditService,
    ICurrentUserService currentUserService,
    ILogger<AuditController> logger) : ControllerBase
{
    [HttpGet("my-activities")]
    [Authorize(Policy = "Audit.ViewOwn")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetMyActivities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var activities = await auditService.GetByUserIdAsync(userId, page, pageSize);
            
            // Apply date filtering if provided
            if (startDate.HasValue || endDate.HasValue)
            {
                activities = activities.Where(a => 
                    (!startDate.HasValue || a.Timestamp >= startDate.Value) &&
                    (!endDate.HasValue || a.Timestamp <= endDate.Value));
            }

            logger.LogInformation("User {UserId} retrieved their audit activities", userId);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user audit activities");
            return StatusCode(500, "An error occurred while retrieving audit activities");
        }
    }

    [HttpGet("all")]
    [Authorize(Policy = "Audit.View")]
    public async Task<ActionResult<PagedDto<AuditLogDto>>> GetAllAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? eventType = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var recentLogs = await auditService.GetRecentAsync(page * pageSize);
            
            // Apply filters
            var filteredLogs = recentLogs.AsEnumerable();
            
            if (!string.IsNullOrEmpty(eventType))
                filteredLogs = filteredLogs.Where(a => a.EventType.Contains(eventType, StringComparison.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(entityType))
                filteredLogs = filteredLogs.Where(a => a.EntityType.Contains(entityType, StringComparison.OrdinalIgnoreCase));
            
            if (startDate.HasValue)
                filteredLogs = filteredLogs.Where(a => a.Timestamp >= startDate.Value);
            
            if (endDate.HasValue)
                filteredLogs = filteredLogs.Where(a => a.Timestamp <= endDate.Value);

            // Apply pagination
            var pagedLogs = filteredLogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PagedDto<AuditLogDto>
            {
                Items = pagedLogs,
                TotalCount = filteredLogs.Count(),
                PageNumber = page,
                PageSize = pageSize
            };

            logger.LogInformation("Audit viewer accessed audit logs");
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving audit logs for audit viewer");
            return StatusCode(500, "An error occurred while retrieving audit logs");
        }
    }

    [HttpGet("recent")]
    [Authorize(Policy = "Audit.View")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetRecentAuditLogs(
        [FromQuery] int count = 100)
    {
        try
        {
            var recentLogs = await auditService.GetRecentAsync(count);
            
            logger.LogInformation("Audit viewer accessed recent audit logs");
            return Ok(recentLogs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving recent audit logs");
            return StatusCode(500, "An error occurred while retrieving recent audit logs");
        }
    }

    [HttpGet("user/{userId}")]
    [Authorize(Policy = "Audit.View")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetUserAuditLogs(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var activities = await auditService.GetByUserIdAsync(userId, page, pageSize);
            
            // Apply date filtering if provided
            if (startDate.HasValue || endDate.HasValue)
            {
                activities = activities.Where(a => 
                    (!startDate.HasValue || a.Timestamp >= startDate.Value) &&
                    (!endDate.HasValue || a.Timestamp <= endDate.Value));
            }

            logger.LogInformation("Audit viewer accessed user {UserId} audit logs", userId);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user audit logs for audit viewer. UserId: {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving user audit logs");
        }
    }

    [HttpGet("document/{docId}")]
    [Authorize(Policy = "Audit.View")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetDocumentAuditLogs(
        string docId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var activities = await auditService.GetByEntityAsync("Document", docId, page, pageSize);
            
            // Apply date filtering if provided
            if (startDate.HasValue || endDate.HasValue)
            {
                activities = activities.Where(a => 
                    (!startDate.HasValue || a.Timestamp >= startDate.Value) &&
                    (!endDate.HasValue || a.Timestamp <= endDate.Value));
            }

            logger.LogInformation("Audit viewer accessed document {DocId} audit logs", docId);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document audit logs for audit viewer. DocId: {DocId}", docId);
            return StatusCode(500, "An error occurred while retrieving document audit logs");
        }
    }

    [HttpGet("event-type/{type}")]
    [Authorize(Policy = "Audit.View")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetEventTypeAuditLogs(
        string type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var activities = await auditService.GetByActionAsync(type, page, pageSize);
            
            // Apply date filtering if provided
            if (startDate.HasValue || endDate.HasValue)
            {
                activities = activities.Where(a => 
                    (!startDate.HasValue || a.Timestamp >= startDate.Value) &&
                    (!endDate.HasValue || a.Timestamp <= endDate.Value));
            }

            logger.LogInformation("Audit viewer accessed event type {Type} audit logs", type);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving event type audit logs for audit viewer. Type: {Type}", type);
            return StatusCode(500, "An error occurred while retrieving event type audit logs");
        }
    }

    [HttpGet("date-range")]
    [Authorize(Policy = "Audit.View")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetDateRangeAuditLogs(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var activities = await auditService.GetByDateRangeAsync(startDate, endDate, page, pageSize);

            logger.LogInformation("Audit viewer accessed date range audit logs from {StartDate} to {EndDate}", 
                startDate, endDate);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving date range audit logs for audit viewer. StartDate: {StartDate}, EndDate: {EndDate}", 
                startDate, endDate);
            return StatusCode(500, "An error occurred while retrieving date range audit logs");
        }
    }

    [HttpGet("statistics")]
    [Authorize(Policy = "Audit.Manage")]
    public async Task<ActionResult<AuditStatisticsDto>> GetAuditStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Get recent logs for statistics
            var recentLogs = await auditService.GetRecentAsync(5000);
            
            // Apply date filtering if provided
            if (startDate.HasValue || endDate.HasValue)
            {
                recentLogs = recentLogs.Where(a => 
                    (!startDate.HasValue || a.Timestamp >= startDate.Value) &&
                    (!endDate.HasValue || a.Timestamp <= endDate.Value));
            }

            // Calculate today's activities
            var today = DateTime.Today;
            var todayActivities = recentLogs.Count(a => a.Timestamp.Date == today);
            // Calculate active users (users who performed activities in the filtered period)
            var activeUsers = recentLogs.Select(a => a.UserId).Distinct().Count();

            // Calculate critical events (delete, error, security related events)
            var criticalEventTypes = new[] { "DocumentDeleted", "UserDeleted", "SecurityBreach", "LoginFailed", "UnauthorizedAccess" };
            var criticalEvents = recentLogs.Count(a => criticalEventTypes.Any(ce => a.EventType.Contains(ce, StringComparison.OrdinalIgnoreCase)));

            // Calculate hourly activity distribution
            var hourlyActivity = new int[24];
            foreach (var log in recentLogs)
            {
                hourlyActivity[log.Timestamp.Hour]++;
            }

            var statistics = new AuditStatisticsDto
            {
                TotalActivities = recentLogs.Count(),
                ActiveUsers = activeUsers,
                TodayActivities = todayActivities,
                CriticalEvents = criticalEvents,
                EventTypeBreakdown = recentLogs
                    .GroupBy(a => a.EventType)
                    .Select(g => new EventTypeBreakdownDto { EventType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList(),
                EntityTypeBreakdown = recentLogs
                    .GroupBy(a => a.EntityType)
                    .Select(g => new EntityTypeBreakdownDto { EntityType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList(),
                DailyActivity = recentLogs
                    .GroupBy(a => a.Timestamp.Date)
                    .Select(g => new DailyActivityDto { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .Take(30)
                    .ToList(),
                HourlyActivity = hourlyActivity,
                TopUsers = recentLogs
                    .GroupBy(a => a.UserId)
                    .Select(g => new TopUserDto { UserId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList(),
                TopActivities = recentLogs
                    .GroupBy(a => a.EventType)
                    .Select((g, index) => new TopActivityDto
                    { 
                        Rank = index + 1,
                        EventType = g.Key, 
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / recentLogs.Count() * 100, 1),
                        Trend = "up" // Bu gerçek trend hesaplaması için daha kompleks logic gerekir
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList()
            };

            logger.LogInformation("Audit manager accessed audit statistics");
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving audit statistics");
            return StatusCode(500, "An error occurred while retrieving audit statistics");
        }
    }
} 