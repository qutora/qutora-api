using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.DTOs.Dashboard;
using Qutora.Shared.Enums;

namespace Qutora.Application.Services
{
    /// <summary>
    /// Dashboard service implementation for managing dashboard statistics and data
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int CacheExpirationMinutes = 5;

        public DashboardService(
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            ILogger<DashboardService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync()
        {
            try
            {
                const string cacheKey = "admin_dashboard_stats";
                
                if (_cache.TryGetValue(cacheKey, out AdminDashboardStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                // Execute queries sequentially to avoid DbContext concurrency issues
                var documentStats = await GetDocumentStatsAsync();
                var userStats = await GetUserStatsAsync();
                var approvalStats = await GetApprovalStatsAsync();
                var apiKeyStats = await GetApiKeyStatsAsync();

                var adminStats = new AdminDashboardStatsDto
                {
                    // Document Statistics
                    TotalDocuments = documentStats.TotalDocuments,
                    MonthlyUploads = documentStats.MonthlyUploads,
                    StorageUsage = documentStats.StorageUsage,

                    // User Statistics
                    TotalUsers = userStats.TotalUsers,
                    ActiveUsers = userStats.ActiveUsers,
                    NewUsersMonth = userStats.NewUsersMonth,
                    AvgSessionTime = userStats.AvgSessionTime,
                    DailyActiveUsers = userStats.DailyActiveUsers,

                    // Approval Statistics
                    PendingApprovals = approvalStats.PendingApprovals,
                    TodayApproved = approvalStats.TodayApproved,
                    UrgentApprovals = approvalStats.UrgentApprovals,
                    AvgApprovalTime = approvalStats.AvgApprovalTime,

                    // API Key Statistics
                    TotalApiKeys = apiKeyStats.TotalApiKeys,
                    ActiveApiKeys = apiKeyStats.ActiveApiKeys,
                    DailyApiCalls = apiKeyStats.DailyApiCalls,
                    ApiErrorRate = apiKeyStats.ApiErrorRate
                };

                _cache.Set(cacheKey, adminStats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return adminStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin dashboard statistics");
                return GetFallbackAdminStats();
            }
        }

        public async Task<UserDashboardStatsDto> GetUserDashboardStatsAsync(string userId)
        {
            try
            {
                var cacheKey = $"user_dashboard_stats_{userId}";
                
                if (_cache.TryGetValue(cacheKey, out UserDashboardStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                // Execute queries sequentially to avoid DbContext concurrency issues
                var userDocStats = await GetUserDocumentStatsAsync(userId);
                var userSharingStats = await GetUserSharingStatsAsync(userId);
                var recentDocs = await GetRecentDocumentsAsync(userId, 4);
                var recentActivities = await GetRecentActivitiesAsync(userId, 5);

                var userStats = new UserDashboardStatsDto
                {
                    // Document Statistics
                    MyDocuments = userDocStats.MyDocuments,
                    MyMonthlyUploads = userDocStats.MyMonthlyUploads,
                    MyStorageUsage = userDocStats.MyStorageUsage,

                    // Sharing Statistics
                    MyShares = userSharingStats.MyShares,
                    MyActiveShares = userSharingStats.MyActiveShares,
                    MyTotalViews = userSharingStats.MyTotalViews,
                    MyWeeklyShares = userSharingStats.MyWeeklyShares,
                    MyAvgViews = userSharingStats.MyAvgViews,

                    // Recent Data
                    RecentDocuments = recentDocs,
                    RecentActivities = recentActivities
                };

                _cache.Set(cacheKey, userStats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return userStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user dashboard statistics for user {UserId}", userId);
                return GetFallbackUserStats();
            }
        }

        public async Task<DocumentStatsDto> GetDocumentStatsAsync()
        {
            try
            {
                const string cacheKey = "document_stats";
                
                if (_cache.TryGetValue(cacheKey, out DocumentStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                var totalDocuments = await _unitOfWork.Documents.CountAsync(d => !d.IsDeleted);
                var monthlyUploads = await _unitOfWork.Documents.CountAsync(d => !d.IsDeleted && d.CreatedAt >= startOfMonth);
                
                var documents = await _unitOfWork.Documents.FindAsync(d => !d.IsDeleted);
                
                var totalSize = documents.Sum(d => d.FileSizeBytes);
                var storageUsage = Math.Round((decimal)totalSize / (1024 * 1024 * 1024), 2); // GB
                
                // Today and week calculations
                var today = DateTime.UtcNow.Date;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var todayUploads = await _unitOfWork.Documents.CountAsync(d => !d.IsDeleted && d.CreatedAt >= today);
                var weeklyUploads = await _unitOfWork.Documents.CountAsync(d => !d.IsDeleted && d.CreatedAt >= startOfWeek);
                
                // Average file size in MB
                var avgFileSize = documents.Any() ? Math.Round((decimal)documents.Average(d => d.FileSizeBytes) / (1024 * 1024), 2) : 0;

                var stats = new DocumentStatsDto
                {
                    TotalDocuments = totalDocuments,
                    MonthlyUploads = monthlyUploads,
                    StorageUsage = storageUsage,
                    TodayUploads = todayUploads,
                    WeeklyUploads = weeklyUploads,
                    AvgFileSize = avgFileSize,
                    MostUsedFileType = "pdf"
                };

                _cache.Set(cacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document statistics");
                return new DocumentStatsDto();
            }
        }
        public async Task<UserDocumentStatsDto> GetUserDocumentStatsAsync(string userId)
        {
            try
            {
                var cacheKey = $"user_document_stats_{userId}";
                
                if (_cache.TryGetValue(cacheKey, out UserDocumentStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var now = DateTime.UtcNow;
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                var myDocuments = await _unitOfWork.Documents.CountAsync(d => d.CreatedBy == userId && !d.IsDeleted);
                
                var myMonthlyUploads = await _unitOfWork.Documents.CountAsync(d => d.CreatedBy == userId && !d.IsDeleted && d.CreatedAt >= startOfMonth);

                var userDocuments = await _unitOfWork.Documents.FindAsync(d => d.CreatedBy == userId && !d.IsDeleted);
                
                var myTotalSize = userDocuments.Sum(d => d.FileSizeBytes);
                var myStorageUsage = Math.Round((decimal)myTotalSize / (1024 * 1024), 2); // MB
                
                // User's today and weekly uploads
                var myTodayUploads = await _unitOfWork.Documents.CountAsync(d => d.CreatedBy == userId && !d.IsDeleted && d.CreatedAt >= today);
                var myWeeklyUploads = await _unitOfWork.Documents.CountAsync(d => d.CreatedBy == userId && !d.IsDeleted && d.CreatedAt >= startOfWeek);
                
                // User's average file size in MB
                var myAvgFileSize = userDocuments.Any() ? Math.Round((decimal)userDocuments.Average(d => d.FileSizeBytes) / (1024 * 1024), 2) : 0;

                var stats = new UserDocumentStatsDto
                {
                    MyDocuments = myDocuments,
                    MyMonthlyUploads = myMonthlyUploads,
                    MyStorageUsage = myStorageUsage,
                    MyTodayUploads = myTodayUploads,
                    MyWeeklyUploads = myWeeklyUploads,
                    MyAvgFileSize = myAvgFileSize,
                    MyMostUsedFileType = "pdf",
                    StorageUsagePercentage = Math.Round((myStorageUsage / 1024) * 100, 1)
                };

                _cache.Set(cacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user document statistics for user {UserId}", userId);
                return new UserDocumentStatsDto();
            }
        }
        public async Task<UserStatsDto> GetUserStatsAsync()
        {
            try
            {
                const string cacheKey = "user_stats";
                
                if (_cache.TryGetValue(cacheKey, out UserStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var now = DateTime.UtcNow;
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var thirtyDaysAgo = now.AddDays(-30);

                var allUsers = _userManager.Users.ToList();
                var totalUsers = allUsers.Count;
                var newUsersMonth = allUsers.Count(u => u.CreatedAt >= startOfMonth);
                var newUsersWeek = allUsers.Count(u => u.CreatedAt >= startOfWeek);
                var newUsersToday = allUsers.Count(u => u.CreatedAt >= today);

                // Active users from audit logs
                var auditLogs = await _unitOfWork.AuditLogs.FindAsync(a => a.Timestamp >= thirtyDaysAgo);
                var activeUsers = auditLogs.Select(a => a.UserId).Distinct().Count();
                
                // Daily and weekly active users
                var dailyAuditLogs = await _unitOfWork.AuditLogs.FindAsync(a => a.Timestamp >= today);
                var weeklyAuditLogs = await _unitOfWork.AuditLogs.FindAsync(a => a.Timestamp >= startOfWeek);
                var dailyActiveUsers = dailyAuditLogs.Select(a => a.UserId).Distinct().Count();
                var weeklyActiveUsers = weeklyAuditLogs.Select(a => a.UserId).Distinct().Count();

                var stats = new UserStatsDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    NewUsersMonth = newUsersMonth,
                    NewUsersWeek = newUsersWeek,
                    NewUsersToday = newUsersToday,
                    AvgSessionTime = "24min",
                    DailyActiveUsers = dailyActiveUsers,
                    WeeklyActiveUsers = weeklyActiveUsers,
                    MonthlyActiveUsers = activeUsers
                };

                _cache.Set(cacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return new UserStatsDto();
            }
        }

        public async Task<ApprovalStatsDto> GetApprovalStatsAsync()
        {
            try
            {
                const string cacheKey = "approval_stats";
                
                if (_cache.TryGetValue(cacheKey, out ApprovalStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var now = DateTime.UtcNow;
                var startOfDay = now.Date;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                // Real approval statistics using repositories
                var pendingApprovals = await _unitOfWork.ShareApprovalRequests.CountAsync(r => r.Status == ApprovalStatus.Pending);
                
                var todayApproved = await _unitOfWork.ApprovalDecisions.CountAsync(d => 
                    d.CreatedAt >= startOfDay && 
                    d.Decision == ApprovalAction.Approved);
                
                var urgentApprovals = await _unitOfWork.ShareApprovalRequests.CountAsync(r => 
                    r.Status == ApprovalStatus.Pending && 
                    r.Priority == Shared.Enums.ApprovalPriority.High);
                
                var totalRequestsMonth = await _unitOfWork.ShareApprovalRequests.CountAsync(r => r.CreatedAt >= startOfMonth);

                // Calculate average approval time
                var approvedThisMonth = await _unitOfWork.ApprovalDecisions.FindAsync(d => 
                    d.CreatedAt >= startOfMonth && 
                    d.Decision == ApprovalAction.Approved);
                
                var avgApprovalTimeHours = 0.0;
                if (approvedThisMonth.Any())
                {
                    var approvalTimes = approvedThisMonth.Select(d => 
                        (d.CreatedAt - d.ShareApprovalRequest.CreatedAt).TotalHours);
                    avgApprovalTimeHours = approvalTimes.Average();
                }

                var stats = new ApprovalStatsDto
                {
                    PendingApprovals = pendingApprovals,
                    TodayApproved = todayApproved,
                    UrgentApprovals = urgentApprovals,
                    AvgApprovalTime = $"{avgApprovalTimeHours:F1}h",
                    TotalRequestsMonth = totalRequestsMonth,
                    TotalRequestsWeek = await _unitOfWork.ShareApprovalRequests.CountAsync(r => r.CreatedAt >= now.AddDays(-7)),
                    ApprovedMonth = await _unitOfWork.ApprovalDecisions.CountAsync(d => 
                        d.CreatedAt >= startOfMonth && 
                        d.Decision == ApprovalAction.Approved),
                    RejectedMonth = await _unitOfWork.ApprovalDecisions.CountAsync(d => 
                        d.CreatedAt >= startOfMonth && 
                        d.Decision == ApprovalAction.Rejected),
                    ApprovalRate = new decimal(totalRequestsMonth > 0 ? 
                        Math.Round((double)await _unitOfWork.ApprovalDecisions.CountAsync(d => 
                            d.CreatedAt >= startOfMonth && 
                            d.Decision == ApprovalAction.Approved) / totalRequestsMonth * 100, 1) : 0),
                    OverdueApprovals = await _unitOfWork.ShareApprovalRequests.CountAsync(r => 
                        r.Status == ApprovalStatus.Pending && 
                        r.CreatedAt < now.AddDays(-3)) // Consider overdue after 3 days
                };

                _cache.Set(cacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting approval statistics");
                return new ApprovalStatsDto();
            }
        }

        public async Task<ApiKeyStatsDto> GetApiKeyStatsAsync()
        {
            try
            {
                const string cacheKey = "api_key_stats";
                
                if (_cache.TryGetValue(cacheKey, out ApiKeyStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var now = DateTime.UtcNow;
                var nextWeek = now.AddDays(7);
                var totalApiKeys = await _unitOfWork.ApiKeys.CountAsync();
                var activeApiKeys = await _unitOfWork.ApiKeys.CountAsync(k => k.IsActive && (!k.ExpiresAt.HasValue || k.ExpiresAt > now));
                var expiredApiKeys = await _unitOfWork.ApiKeys.CountAsync(k => k.ExpiresAt.HasValue && k.ExpiresAt <= now);
                var expiringApiKeys = await _unitOfWork.ApiKeys.CountAsync(k => k.ExpiresAt.HasValue && k.ExpiresAt > now && k.ExpiresAt <= nextWeek);

                var stats = new ApiKeyStatsDto
                {
                    TotalApiKeys = totalApiKeys,
                    ActiveApiKeys = activeApiKeys,
                    DailyApiCalls = 0, // TODO: Implement API call tracking
                    WeeklyApiCalls = 0, // TODO: Implement API call tracking
                    MonthlyApiCalls = 0, // TODO: Implement API call tracking
                    ApiErrorRate = 0, // TODO: Implement error rate tracking
                    ExpiredApiKeys = expiredApiKeys,
                    ExpiringApiKeys = expiringApiKeys,
                    TopUsedKeys = new List<TopApiKeyUsageDto>()
                };

                _cache.Set(cacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key statistics");
                return new ApiKeyStatsDto();
            }
        }

        public async Task<UserApiKeyStatsDto> GetUserApiKeyStatsAsync(string userId)
        {
            try
            {
                var cacheKey = $"user_api_key_stats_{userId}";
                
                if (_cache.TryGetValue(cacheKey, out UserApiKeyStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var now = DateTime.UtcNow;
                var nextWeek = now.AddDays(7);
                var myApiKeys = await _unitOfWork.ApiKeys.CountAsync(k => k.CreatedBy == userId);
                var myActiveApiKeys = await _unitOfWork.ApiKeys.CountAsync(k => k.CreatedBy == userId && k.IsActive && (!k.ExpiresAt.HasValue || k.ExpiresAt > now));
                var myExpiredApiKeys = await _unitOfWork.ApiKeys.CountAsync(k => k.CreatedBy == userId && k.ExpiresAt.HasValue && k.ExpiresAt <= now);
                var myExpiringApiKeys = await _unitOfWork.ApiKeys.CountAsync(k => k.CreatedBy == userId && k.ExpiresAt.HasValue && k.ExpiresAt > now && k.ExpiresAt <= nextWeek);

                var stats = new UserApiKeyStatsDto
                {
                    MyApiKeys = myApiKeys,
                    MyActiveApiKeys = myActiveApiKeys,
                    MyDailyApiCalls = 0, // TODO: Implement user API call tracking
                    MyWeeklyApiCalls = 0, // TODO: Implement user API call tracking
                    MyMonthlyApiCalls = 0, // TODO: Implement user API call tracking
                    MyApiErrorRate = 0, // TODO: Implement user error rate tracking
                    MyExpiredApiKeys = myExpiredApiKeys,
                    MyExpiringApiKeys = myExpiringApiKeys,
                    MyTopUsedKeys = new List<TopApiKeyUsageDto>()
                };

                _cache.Set(cacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user API key statistics for user {UserId}", userId);
                return new UserApiKeyStatsDto();
            }
        }

        public async Task<SharingStatsDto> GetSharingStatsAsync()
        {
            try
            {
                const string cacheKey = "sharing_stats";
                
                if (_cache.TryGetValue(cacheKey, out SharingStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var now = DateTime.UtcNow;
                var today = DateTime.UtcNow.Date;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                
                var totalShares = await _unitOfWork.DocumentShares.CountAsync();
                var activeShares = await _unitOfWork.DocumentShares.CountAsync(s => s.IsActive && (!s.ExpiresAt.HasValue || s.ExpiresAt > now));
                var todayShares = await _unitOfWork.DocumentShares.CountAsync(s => s.CreatedAt >= today);
                var weeklyShares = await _unitOfWork.DocumentShares.CountAsync(s => s.CreatedAt >= startOfWeek);
                var monthlyShares = await _unitOfWork.DocumentShares.CountAsync(s => s.CreatedAt >= startOfMonth);

                var stats = new SharingStatsDto
                {
                    TotalShares = totalShares,
                    ActiveShares = activeShares,
                    TotalViews = 0, // TODO: Implement view tracking
                    TodayShares = todayShares,
                    WeeklyShares = weeklyShares,
                    MonthlyShares = monthlyShares,
                    TodayViews = 0,
                    WeeklyViews = 0,
                    MonthlyViews = 0,
                    AvgViewsPerShare = 0,
                    TopSharedDocuments = new List<TopSharedDocumentDto>()
                };

                _cache.Set(cacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sharing statistics");
                return new SharingStatsDto();
            }
        }

        public async Task<UserSharingStatsDto> GetUserSharingStatsAsync(string userId)
        {
            try
            {
                var cacheKey = $"user_sharing_stats_{userId}";
                
                if (_cache.TryGetValue(cacheKey, out UserSharingStatsDto? cachedStats))
                {
                    return cachedStats!;
                }

                var now = DateTime.UtcNow;
                var today = DateTime.UtcNow.Date;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                
                var myShares = await _unitOfWork.DocumentShares.CountAsync(s => s.CreatedBy == userId);
                var myActiveShares = await _unitOfWork.DocumentShares.CountAsync(s => s.CreatedBy == userId && s.IsActive && (!s.ExpiresAt.HasValue || s.ExpiresAt > now));
                var myTodayShares = await _unitOfWork.DocumentShares.CountAsync(s => s.CreatedBy == userId && s.CreatedAt >= today);
                var myWeeklyShares = await _unitOfWork.DocumentShares.CountAsync(s => s.CreatedBy == userId && s.CreatedAt >= startOfWeek);
                var myMonthlyShares = await _unitOfWork.DocumentShares.CountAsync(s => s.CreatedBy == userId && s.CreatedAt >= startOfMonth);

                var stats = new UserSharingStatsDto
                {
                    MyShares = myShares,
                    MyActiveShares = myActiveShares,
                    MyTotalViews = 0, // TODO: Implement view tracking for user
                    MyTodayShares = myTodayShares,
                    MyWeeklyShares = myWeeklyShares,
                    MyMonthlyShares = myMonthlyShares,
                    MyTodayViews = 0,
                    MyWeeklyViews = 0,
                    MyMonthlyViews = 0,
                    MyAvgViews = 0,
                    MyTopSharedDocuments = new List<TopSharedDocumentDto>()
                };

                _cache.Set(cacheKey, stats, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user sharing statistics for user {UserId}", userId);
                return new UserSharingStatsDto();
            }
        }

        public async Task<List<RecentDocumentDto>> GetRecentDocumentsAsync(string userId, int limit = 5)
        {
            try
            {
                var cacheKey = $"recent_documents_{userId}_{limit}";
                
                if (_cache.TryGetValue(cacheKey, out List<RecentDocumentDto>? cachedDocs))
                {
                    return cachedDocs!;
                }

                var userDocuments = await _unitOfWork.Documents.FindAsync(d => d.CreatedBy == userId && !d.IsDeleted);
                var recentDocs = userDocuments
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(limit)
                    .Select(d => new RecentDocumentDto
                    {
                        Id = d.Id.ToString(),
                        Name = d.Name,
                        UploadedAt = d.CreatedAt,
                        FileType = d.ContentType ?? "unknown",
                        FileSize = FormatFileSize(d.FileSizeBytes),
                        Category = "Uncategorized", // Category loading için ayrı query gerekli
                        ThumbnailUrl = string.Empty
                    })
                    .ToList();

                _cache.Set(cacheKey, recentDocs, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return recentDocs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent documents for user {UserId}", userId);
                return new List<RecentDocumentDto>();
            }
        }

        public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(string userId, int limit = 5)
        {
            try
            {
                var cacheKey = $"recent_activities_{userId}_{limit}";
                
                if (_cache.TryGetValue(cacheKey, out List<RecentActivityDto>? cachedActivities))
                {
                    return cachedActivities!;
                }

                var userAuditLogs = await _unitOfWork.AuditLogs.FindAsync(a => a.UserId == userId);
                var recentActivities = userAuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(limit)
                    .Select(a => new RecentActivityDto
                    {
                        Id = a.Id.ToString(),
                        Action = a.EventType,
                        Description = GetActivityDescription(a.EventType),
                        Timestamp = a.Timestamp,
                        IconClass = GetActivityIcon(a.EventType),
                        ColorClass = GetActivityColor(a.EventType),
                        EntityId = a.EntityId ?? string.Empty,
                        EntityName = a.Description
                    })
                    .ToList();

                _cache.Set(cacheKey, recentActivities, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                    Size = 1
                });
                return recentActivities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities for user {UserId}", userId);
                return new List<RecentActivityDto>();
            }
        }

        #region Private Helper Methods

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{Math.Round((decimal)bytes / 1024, 1)} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{Math.Round((decimal)bytes / (1024 * 1024), 1)} MB";
            return $"{Math.Round((decimal)bytes / (1024 * 1024 * 1024), 1)} GB";
        }

        private static string GetActivityDescription(string action)
        {
            return action switch
            {
                var a when a.Contains("Document") && a.Contains("Upload") => "New document uploaded",
                var a when a.Contains("Document") && a.Contains("Update") => "Document updated",
                var a when a.Contains("Document") && a.Contains("Delete") => "Document deleted",
                var a when a.Contains("DocumentShare") && a.Contains("Create") => "Document shared",
                var a when a.Contains("ApiKey") && a.Contains("Create") => "API key created",
                var a when a.Contains("Approval") && a.Contains("Request") => "Approval requested",
                var a when a.Contains("Login") => "Logged in",
                _ => "Activity performed"
            };
        }

        private static string GetActivityIcon(string action)
        {
            return action switch
            {
                var a when a.Contains("Document") && a.Contains("Upload") => "bi-file-plus",
                var a when a.Contains("Document") && a.Contains("Update") => "bi-file-earmark-text",
                var a when a.Contains("Document") && a.Contains("Delete") => "bi-file-minus",
                var a when a.Contains("DocumentShare") => "bi-share",
                var a when a.Contains("ApiKey") => "bi-key",
                var a when a.Contains("Approval") => "bi-check-circle",
                var a when a.Contains("Login") => "bi-box-arrow-in-right",
                _ => "bi-activity"
            };
        }

        private static string GetActivityColor(string action)
        {
            return action switch
            {
                var a when a.Contains("Upload") || a.Contains("Create") => "text-success",
                var a when a.Contains("Update") => "text-primary",
                var a when a.Contains("Delete") => "text-danger",
                var a when a.Contains("Share") => "text-info",
                var a when a.Contains("Login") => "text-secondary",
                _ => "text-muted"
            };
        }


        private static AdminDashboardStatsDto GetFallbackAdminStats()
        {
            return new AdminDashboardStatsDto
            {
                TotalDocuments = 0,
                MonthlyUploads = 0,
                StorageUsage = 0,
                TotalUsers = 0,
                ActiveUsers = 0,
                NewUsersMonth = 0,
                AvgSessionTime = "0min",
                DailyActiveUsers = 0,
                PendingApprovals = 0,
                TodayApproved = 0,
                UrgentApprovals = 0,
                AvgApprovalTime = "0h",
                TotalApiKeys = 0,
                ActiveApiKeys = 0,
                DailyApiCalls = 0,
                ApiErrorRate = 0
            };
        }

        private static UserDashboardStatsDto GetFallbackUserStats()
        {
            return new UserDashboardStatsDto
            {
                MyDocuments = 0,
                MyMonthlyUploads = 0,
                MyStorageUsage = 0,
                MyShares = 0,
                MyActiveShares = 0,
                MyTotalViews = 0,
                MyWeeklyShares = 0,
                MyAvgViews = 0,
                RecentDocuments = new List<RecentDocumentDto>(),
                RecentActivities = new List<RecentActivityDto>()
            };
        }

        #endregion
    }
} 
