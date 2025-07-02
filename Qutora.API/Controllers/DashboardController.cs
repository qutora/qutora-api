using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;

namespace Qutora.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ICurrentUserService currentUserService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Get dashboard statistics based on user role
        /// </summary>
        /// <returns>Dashboard statistics</returns>
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var userId = _currentUserService.UserId;
                var isAdmin = _currentUserService.IsInRole("Admin");

                if (isAdmin)
                {
                    var adminStats = await _dashboardService.GetAdminDashboardStatsAsync();
                    return Ok(adminStats);
                }
                else
                {
                    var userStats = await _dashboardService.GetUserDashboardStatsAsync(userId);
                    return Ok(userStats);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats for user {UserId}", _currentUserService.UserId);
                return StatusCode(500, "An error occurred while loading dashboard data");
            }
        }

        /// <summary>
        /// Get document statistics (Admin only)
        /// </summary>
        [HttpGet("document-stats")]
        [Authorize(Policy = "Admin.Access")]
        public async Task<IActionResult> GetDocumentStats()
        {
            try
            {
                var stats = await _dashboardService.GetDocumentStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document stats");
                return StatusCode(500, "An error occurred while loading document statistics");
            }
        }

        /// <summary>
        /// Get user-specific document statistics
        /// </summary>
        [HttpGet("user-document-stats")]
        public async Task<IActionResult> GetUserDocumentStats()
        {
            try
            {
                var userId = _currentUserService.UserId;
                var stats = await _dashboardService.GetUserDocumentStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user document stats for user {UserId}", _currentUserService.UserId);
                return StatusCode(500, "An error occurred while loading user document statistics");
            }
        }

        /// <summary>
        /// Get user statistics (Admin only)
        /// </summary>
        [HttpGet("user-stats")]
        [Authorize(Policy = "Admin.Access")]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var stats = await _dashboardService.GetUserStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user stats");
                return StatusCode(500, "An error occurred while loading user statistics");
            }
        }

        /// <summary>
        /// Get approval statistics (Admin only)
        /// </summary>
        [HttpGet("approval-stats")]
        [Authorize(Policy = "Admin.Access")]
        public async Task<IActionResult> GetApprovalStats()
        {
            try
            {
                var stats = await _dashboardService.GetApprovalStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval stats");
                return StatusCode(500, "An error occurred while loading approval statistics");
            }
        }

        /// <summary>
        /// Get API key statistics (Admin only)
        /// </summary>
        [HttpGet("api-key-stats")]
        [Authorize(Policy = "Admin.Access")]
        public async Task<IActionResult> GetApiKeyStats()
        {
            try
            {
                var stats = await _dashboardService.GetApiKeyStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key stats");
                return StatusCode(500, "An error occurred while loading API key statistics");
            }
        }

        /// <summary>
        /// Get user-specific API key statistics
        /// </summary>
        [HttpGet("user-api-key-stats")]
        public async Task<IActionResult> GetUserApiKeyStats()
        {
            try
            {
                var userId = _currentUserService.UserId;
                var stats = await _dashboardService.GetUserApiKeyStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user API key stats for user {UserId}", _currentUserService.UserId);
                return StatusCode(500, "An error occurred while loading user API key statistics");
            }
        }

        /// <summary>
        /// Get sharing statistics (Admin only)
        /// </summary>
        [HttpGet("sharing-stats")]
        [Authorize(Policy = "Admin.Access")]
        public async Task<IActionResult> GetSharingStats()
        {
            try
            {
                var stats = await _dashboardService.GetSharingStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sharing stats");
                return StatusCode(500, "An error occurred while loading sharing statistics");
            }
        }

        /// <summary>
        /// Get user-specific sharing statistics
        /// </summary>
        [HttpGet("user-sharing-stats")]
        public async Task<IActionResult> GetUserSharingStats()
        {
            try
            {
                var userId = _currentUserService.UserId;
                var stats = await _dashboardService.GetUserSharingStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sharing stats for user {UserId}", _currentUserService.UserId);
                return StatusCode(500, "An error occurred while loading user sharing statistics");
            }
        }

        /// <summary>
        /// Get recent documents for user
        /// </summary>
        [HttpGet("recent-documents")]
        public async Task<IActionResult> GetRecentDocuments([FromQuery] int limit = 5)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var documents = await _dashboardService.GetRecentDocumentsAsync(userId, limit);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent documents for user {UserId}", _currentUserService.UserId);
                return StatusCode(500, "An error occurred while loading recent documents");
            }
        }

        /// <summary>
        /// Get recent activities for user
        /// </summary>
        [HttpGet("recent-activities")]
        public async Task<IActionResult> GetRecentActivities([FromQuery] int limit = 5)
        {
            try
            {
                var userId = _currentUserService.UserId;
                var activities = await _dashboardService.GetRecentActivitiesAsync(userId, limit);
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities for user {UserId}", _currentUserService.UserId);
                return StatusCode(500, "An error occurred while loading recent activities");
            }
        }
    }
} 