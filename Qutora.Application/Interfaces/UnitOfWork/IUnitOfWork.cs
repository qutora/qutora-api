using Microsoft.EntityFrameworkCore.Storage;
using Qutora.Application.Interfaces.Repositories;

namespace Qutora.Application.Interfaces.UnitOfWork;

/// <summary>
/// Unit of Work interface for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Document repository
    /// </summary>
    IDocumentRepository Documents { get; }

    /// <summary>
    /// Category repository
    /// </summary>
    ICategoryRepository Categories { get; }

    /// <summary>
    /// Storage Provider repository
    /// </summary>
    IStorageProviderRepository StorageProviders { get; }

    /// <summary>
    /// API Key repository
    /// </summary>
    IApiKeyRepository ApiKeys { get; }

    /// <summary>
    /// Metadata repository
    /// </summary>
    IMetadataRepository Metadata { get; }

    /// <summary>
    /// Metadata Schema repository
    /// </summary>
    IMetadataSchemaRepository MetadataSchemas { get; }

    /// <summary>
    /// Document Version repository
    /// </summary>
    IDocumentVersionRepository DocumentVersions { get; }

    /// <summary>
    /// Audit Log repository
    /// </summary>
    IAuditLogRepository AuditLogs { get; }

    /// <summary>
    /// Storage Bucket repository
    /// </summary>
    IStorageBucketRepository StorageBuckets { get; }

    /// <summary>
    /// Bucket Permission repository
    /// </summary>
    IBucketPermissionRepository BucketPermissions { get; }

    /// <summary>
    /// API Key Bucket Permission repository
    /// </summary>
    IApiKeyBucketPermissionRepository ApiKeyBucketPermissions { get; }

    /// <summary>
    /// Document Share repository
    /// </summary>
    IDocumentShareRepository DocumentShares { get; }

    /// <summary>
    /// Document Share View repository
    /// </summary>
    IDocumentShareViewRepository DocumentShareViews { get; }

    /// <summary>
    /// Approval Settings repository
    /// </summary>
    IApprovalSettingsRepository ApprovalSettings { get; }

    /// <summary>
    /// Approval Policy repository
    /// </summary>
    IApprovalPolicyRepository ApprovalPolicies { get; }

    /// <summary>
    /// Share Approval Request repository
    /// </summary>
    IShareApprovalRequestRepository ShareApprovalRequests { get; }

    /// <summary>
    /// Approval Decision repository
    /// </summary>
    IApprovalDecisionRepository ApprovalDecisions { get; }

    /// <summary>
    /// Approval History repository
    /// </summary>
    IApprovalHistoryRepository ApprovalHistories { get; }

    /// <summary>
    /// Email Settings repository
    /// </summary>
    IEmailSettingsRepository EmailSettings { get; }

    /// <summary>
    /// Email Template repository
    /// </summary>
    IEmailTemplateRepository EmailTemplates { get; }

    /// <summary>
    /// Refresh Token repository
    /// </summary>
    IRefreshTokenRepository RefreshTokens { get; }

    /// <summary>
    /// System Settings repository
    /// </summary>
    ISystemSettingsRepository SystemSettings { get; }

    /// <summary>
    /// Creates and returns a database execution strategy for transaction handling
    /// </summary>
    /// <returns>Database execution strategy</returns>
    IExecutionStrategy CreateExecutionStrategy();

    /// <summary>
    /// Executes a function within a retry-enabled transaction scope
    /// </summary>
    /// <param name="operation">Operation to execute within the transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task ExecuteTransactionalAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function within a retry-enabled transaction scope with a return value
    /// </summary>
    /// <typeparam name="TResult">Type of the result</typeparam>
    /// <param name="operation">Operation to execute within the transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<TResult> ExecuteTransactionalAsync<TResult>(Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save all changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected records</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin a new transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles concurrency conflicts when saving changes
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <param name="entity">Entity with changes</param>
    /// <param name="getUpdatedEntity">Function to get updated entity from external source</param>
    /// <param name="handleConflict">Function to resolve conflict between client and database versions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if saved successfully, false if conflict couldn't be resolved</returns>
    Task<bool> TryResolveConcurrencyConflictAsync<TEntity>(
        TEntity entity,
        Func<Task<TEntity>> getUpdatedEntity,
        Func<TEntity, TEntity, Task<TEntity>> handleConflict,
        CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>
    /// Detects optimistic concurrency conflicts and returns information
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <param name="entity">Entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conflict information, database entity and current entity</returns>
    Task<(bool HasConflict, TEntity? DatabaseEntity, TEntity? CurrentEntity)> DetectConcurrencyConflictAsync<TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>
    /// Checks if there is an active transaction
    /// </summary>
    /// <returns>True if transaction exists, false otherwise</returns>
    bool IsInTransaction();
}