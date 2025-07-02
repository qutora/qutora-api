using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Qutora.Application.Identity;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.Repositories;
using Qutora.Infrastructure.Persistence;
using Qutora.Shared.DTOs.Dashboard;

namespace Qutora.Application.Services
{
    /// <summary>
    /// Dashboard service implementation for managing dashboard statistics and data
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private readonly IDocumentRepository _documentRepository;
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly IShareApprovalRequestRepository _approvalRepository;
        private readonly IDocumentShareRepository _documentShareRepository;

        private const int CacheExpirationMinutes = 5;

        public DashboardService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<DashboardService> logger,
            IDocumentRepository documentRepository,
            IApiKeyRepository apiKeyRepository,
            IShareApprovalRequestRepository approvalRepository,
            IDocumentShareRepository documentShareRepository)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _documentRepository = documentRepository;
            _apiKeyRepository = apiKeyRepository;
            _approvalRepository = approvalRepository;
            _documentShareRepository = documentShareRepository;
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

                var totalDocuments = await _context.Documents.Where(d => !d.IsDeleted).CountAsync();
                var monthlyUploads = await _context.Documents
                    .Where(d => !d.IsDeleted && d.CreatedAt >= startOfMonth)
                    .CountAsync();
                
                var documents = await _context.Documents
                    .Where(d => !d.IsDeleted)
                    .ToListAsync();
                
                var totalSize = documents.Sum(d => d.FileSizeBytes);
                
                var storageUsage = Math.Round((decimal)totalSize / (1024 * 1024 * 1024), 2); // GB

                var stats = new DocumentStatsDto
                {
                    TotalDocuments = totalDocuments,
                    MonthlyUploads = monthlyUploads,
                    StorageUsage = storageUsage,
                    TodayUploads = 0,
                    WeeklyUploads = 0,
                    AvgFileSize = 0,
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
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                var myDocuments = await _context.Documents
                    .Where(d => d.CreatedBy == userId && !d.IsDeleted)
                    .CountAsync();
                
                var myMonthlyUploads = await _context.Documents
                    .Where(d => d.CreatedBy == userId && !d.IsDeleted && d.CreatedAt >= startOfMonth)
                    .CountAsync();

                var userDocuments = await _context.Documents
                    .Where(d => d.CreatedBy == userId && !d.IsDeleted)
                    .ToListAsync();
                
                var myTotalSize = userDocuments.Sum(d => d.FileSizeBytes);
                
                var myStorageUsage = Math.Round((decimal)myTotalSize / (1024 * 1024), 2); // MB

                var stats = new UserDocumentStatsDto
                {
                    MyDocuments = myDocuments,
                    MyMonthlyUploads = myMonthlyUploads,
                    MyStorageUsage = myStorageUsage,
                    MyTodayUploads = 0,
                    MyWeeklyUploads = 0,
                    MyAvgFileSize = 0,
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
                var startOfMonth = new DateTime(now.Year, now.Month, 1);
                var thirtyDaysAgo = now.AddDays(-30);

                var totalUsers = await _context.Users.CountAsync();
                var newUsersMonth = await _context.Users
                    .Where(u => u.CreatedAt >= startOfMonth)
                    .CountAsync();

                // Active users from audit logs
                var activeUsers = await _context.AuditLogs
                    .Where(a => a.Timestamp >= thirtyDaysAgo)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

                var stats = new UserStatsDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    NewUsersMonth = newUsersMonth,
                    NewUsersWeek = 0,
                    NewUsersToday = 0,
                    AvgSessionTime = "24min",
                    DailyActiveUsers = 0,
                    WeeklyActiveUsers = 0,
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

                // Basic implementation - will enhance later
                var stats = new ApprovalStatsDto
                {
                    PendingApprovals = 0,
                    TodayApproved = 0,
                    UrgentApprovals = 0,
                    AvgApprovalTime = "0h",
                    TotalRequestsMonth = 0,
                    TotalRequestsWeek = 0,
                    ApprovedMonth = 0,
                    RejectedMonth = 0,
                    ApprovalRate = 0,
                    OverdueApprovals = 0
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
                var totalApiKeys = await _context.ApiKeys.CountAsync();
                var activeApiKeys = await _context.ApiKeys
                    .Where(k => k.IsActive && (!k.ExpiresAt.HasValue || k.ExpiresAt > now))
                    .CountAsync();

                var stats = new ApiKeyStatsDto
                {
                    TotalApiKeys = totalApiKeys,
                    ActiveApiKeys = activeApiKeys,
                    DailyApiCalls = 0,
                    WeeklyApiCalls = 0,
                    MonthlyApiCalls = 0,
                    ApiErrorRate = 0,
                    ExpiredApiKeys = 0,
                    ExpiringApiKeys = 0,
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
                var myApiKeys = await _context.ApiKeys
                    .Where(k => k.CreatedBy == userId)
                    .CountAsync();
                
                var myActiveApiKeys = await _context.ApiKeys
                    .Where(k => k.CreatedBy == userId && k.IsActive && (!k.ExpiresAt.HasValue || k.ExpiresAt > now))
                    .CountAsync();

                var stats = new UserApiKeyStatsDto
                {
                    MyApiKeys = myApiKeys,
                    MyActiveApiKeys = myActiveApiKeys,
                    MyDailyApiCalls = 0,
                    MyWeeklyApiCalls = 0,
                    MyMonthlyApiCalls = 0,
                    MyApiErrorRate = 0,
                    MyExpiredApiKeys = 0,
                    MyExpiringApiKeys = 0,
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
                var totalShares = await _context.DocumentShares.CountAsync();
                var activeShares = await _context.DocumentShares
                    .Where(s => s.IsActive && (!s.ExpiresAt.HasValue || s.ExpiresAt > now))
                    .CountAsync();

                var stats = new SharingStatsDto
                {
                    TotalShares = totalShares,
                    ActiveShares = activeShares,
                    TotalViews = 0,
                    TodayShares = 0,
                    WeeklyShares = 0,
                    MonthlyShares = 0,
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
                var myShares = await _context.DocumentShares
                    .Where(s => s.CreatedBy == userId)
                    .CountAsync();
                
                var myActiveShares = await _context.DocumentShares
                    .Where(s => s.CreatedBy == userId && s.IsActive && (!s.ExpiresAt.HasValue || s.ExpiresAt > now))
                    .CountAsync();

                var stats = new UserSharingStatsDto
                {
                    MyShares = myShares,
                    MyActiveShares = myActiveShares,
                    MyTotalViews = 0,
                    MyTodayShares = 0,
                    MyWeeklyShares = 0,
                    MyMonthlyShares = 0,
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

                var recentDocs = await _context.Documents
                    .Where(d => d.CreatedBy == userId && !d.IsDeleted)
                    .Include(d => d.Category)
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(limit)
                    .Select(d => new RecentDocumentDto
                    {
                        Id = d.Id.ToString(),
                        Name = d.Name,
                        UploadedAt = d.CreatedAt,
                        FileType = d.ContentType ?? "unknown",
                        FileSize = FormatFileSize(d.FileSizeBytes),
                        Category = d.Category != null ? d.Category.Name : "Uncategorized",
                        ThumbnailUrl = string.Empty
                    })
                    .ToListAsync();

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

                var recentActivities = await _context.AuditLogs
                    .Where(a => a.UserId == userId)
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
                    .ToListAsync();

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