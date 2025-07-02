using Qutora.Shared.DTOs.Dashboard;

namespace Qutora.Application.Interfaces
{
    /// <summary>
    /// Dashboard service interface for managing dashboard statistics and data
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Get comprehensive admin dashboard statistics
        /// </summary>
        /// <returns>Admin dashboard statistics</returns>
        Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync();

        /// <summary>
        /// Get comprehensive user dashboard statistics
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User dashboard statistics</returns>
        Task<UserDashboardStatsDto> GetUserDashboardStatsAsync(string userId);

        /// <summary>
        /// Get document statistics (admin only)
        /// </summary>
        /// <returns>Document statistics</returns>
        Task<DocumentStatsDto> GetDocumentStatsAsync();

        /// <summary>
        /// Get user-specific document statistics
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User document statistics</returns>
        Task<UserDocumentStatsDto> GetUserDocumentStatsAsync(string userId);

        /// <summary>
        /// Get user statistics (admin only)
        /// </summary>
        /// <returns>User statistics</returns>
        Task<UserStatsDto> GetUserStatsAsync();

        /// <summary>
        /// Get approval system statistics (admin only)
        /// </summary>
        /// <returns>Approval statistics</returns>
        Task<ApprovalStatsDto> GetApprovalStatsAsync();

        /// <summary>
        /// Get API key statistics (admin only)
        /// </summary>
        /// <returns>API key statistics</returns>
        Task<ApiKeyStatsDto> GetApiKeyStatsAsync();

        /// <summary>
        /// Get user-specific API key statistics
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User API key statistics</returns>
        Task<UserApiKeyStatsDto> GetUserApiKeyStatsAsync(string userId);

        /// <summary>
        /// Get sharing statistics (admin only)
        /// </summary>
        /// <returns>Sharing statistics</returns>
        Task<SharingStatsDto> GetSharingStatsAsync();

        /// <summary>
        /// Get user-specific sharing statistics
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>User sharing statistics</returns>
        Task<UserSharingStatsDto> GetUserSharingStatsAsync(string userId);

        /// <summary>
        /// Get recent documents for user
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="limit">Number of documents to return</param>
        /// <returns>List of recent documents</returns>
        Task<List<RecentDocumentDto>> GetRecentDocumentsAsync(string userId, int limit = 5);

        /// <summary>
        /// Get recent activities for user
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <param name="limit">Number of activities to return</param>
        /// <returns>List of recent activities</returns>
        Task<List<RecentActivityDto>> GetRecentActivitiesAsync(string userId, int limit = 5);
    }
} 