using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.DocumentOrchestration;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service for document storage operations
/// </summary>
public interface IDocumentStorageService
{
    /// <summary>
    /// Selects optimal storage for user
    /// </summary>
    Task<StorageSelectionResult> SelectOptimalStorageAsync(string userId, Guid? providerId, Guid? bucketId);
    
    /// <summary>
    /// Gets user accessible providers
    /// </summary>
    Task<IEnumerable<StorageProviderDto>> GetUserAccessibleProvidersAsync(string userId);
    
    /// <summary>
    /// Gets user accessible buckets for provider
    /// </summary>
    Task<IEnumerable<BucketInfoDto>> GetUserAccessibleBucketsAsync(string userId, Guid? providerId);
} 