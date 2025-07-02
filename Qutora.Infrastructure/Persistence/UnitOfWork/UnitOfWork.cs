using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Qutora.Database.Abstractions;
using Qutora.Infrastructure.Interfaces.Repositories;
using Qutora.Infrastructure.Interfaces.UnitOfWork;
using Qutora.Infrastructure.Persistence.Transactions;

namespace Qutora.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// Implementation of Unit of Work pattern for the application
/// </summary>
public class UnitOfWork(
    ApplicationDbContext context,
    IDbProvider dbProvider,
    ILoggerFactory loggerFactory,
    ITransactionManager transactionManager)
    : IUnitOfWork
{
    private readonly IDbProviderUnitOfWork _unitOfWork =
        new DbProviderUnitOfWork(context, dbProvider, loggerFactory, transactionManager);

    private readonly ILogger<UnitOfWork> _logger = loggerFactory.CreateLogger<UnitOfWork>();
    private bool _disposed;

    public IDbProvider DbProvider => _unitOfWork.DbProvider;

    public IDocumentRepository Documents => _unitOfWork.Documents;

    public ICategoryRepository Categories => _unitOfWork.Categories;

    public IStorageProviderRepository StorageProviders => _unitOfWork.StorageProviders;

    public IApiKeyRepository ApiKeys => _unitOfWork.ApiKeys;

    public IMetadataRepository Metadata => _unitOfWork.Metadata;

    public IMetadataSchemaRepository MetadataSchemas => _unitOfWork.MetadataSchemas;

    public IDocumentVersionRepository DocumentVersions => _unitOfWork.DocumentVersions;

    public IAuditLogRepository AuditLogs => _unitOfWork.AuditLogs;

    public IStorageBucketRepository StorageBuckets => _unitOfWork.StorageBuckets;

    public IBucketPermissionRepository BucketPermissions => _unitOfWork.BucketPermissions;

    public IApiKeyBucketPermissionRepository ApiKeyBucketPermissions => _unitOfWork.ApiKeyBucketPermissions;

    public IDocumentShareRepository DocumentShares => _unitOfWork.DocumentShares;

    public IDocumentShareViewRepository DocumentShareViews => _unitOfWork.DocumentShareViews;

    public IApprovalSettingsRepository ApprovalSettings => _unitOfWork.ApprovalSettings;

    public IApprovalPolicyRepository ApprovalPolicies => _unitOfWork.ApprovalPolicies;

    public IShareApprovalRequestRepository ShareApprovalRequests => _unitOfWork.ShareApprovalRequests;

    public IApprovalDecisionRepository ApprovalDecisions => _unitOfWork.ApprovalDecisions;

    public IApprovalHistoryRepository ApprovalHistories => _unitOfWork.ApprovalHistories;

    public IEmailSettingsRepository EmailSettings => _unitOfWork.EmailSettings;

    public IEmailTemplateRepository EmailTemplates => _unitOfWork.EmailTemplates;

    /// <summary>
    /// Returns the database execution strategy for use in transaction handling
    /// </summary>
    public IExecutionStrategy CreateExecutionStrategy()
    {
        return _unitOfWork.CreateExecutionStrategy();
    }

    /// <summary>
    /// Executes a function within a retry-enabled transaction scope
    /// </summary>
    public async Task ExecuteTransactionalAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.ExecuteTransactionalAsync(operation, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during transactional operation execution");
            throw;
        }
    }

    /// <summary>
    /// Executes a function within a retry-enabled transaction scope with a return value
    /// </summary>
    public async Task<TResult> ExecuteTransactionalAsync<TResult>(Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unitOfWork.ExecuteTransactionalAsync(operation, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during transactional operation execution with return value");
            throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving changes");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while starting transaction");
            throw;
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while committing transaction");
            throw;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while rolling back transaction");
            throw;
        }
    }

    /// <summary>
    /// Manages concurrency conflicts
    /// </summary>
    public async Task<bool> TryResolveConcurrencyConflictAsync<TEntity>(
        TEntity entity,
        Func<Task<TEntity>> getUpdatedEntity,
        Func<TEntity, TEntity, Task<TEntity>> handleConflict,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        try
        {
            return await ((DbProviderUnitOfWork)_unitOfWork).TryResolveConcurrencyConflictAsync(
                entity, getUpdatedEntity, handleConflict, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Concurrency error occurred");
            throw;
        }
    }

    /// <summary>
    /// Detects optimistic concurrency conflicts
    /// </summary>
    public async Task<(bool HasConflict, TEntity? DatabaseEntity, TEntity? CurrentEntity)>
        DetectConcurrencyConflictAsync<TEntity>(
            TEntity entity,
            CancellationToken cancellationToken = default) where TEntity : class
    {
        try
        {
            return await ((DbProviderUnitOfWork)_unitOfWork).DetectConcurrencyConflictAsync(entity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during concurrency conflict detection");
            throw;
        }
    }

    /// <summary>
    /// Checks if there is an active transaction
    /// </summary>
    public bool IsInTransaction()
    {
        return _unitOfWork.IsInTransaction();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _unitOfWork.Dispose();
            _disposed = true;
        }
    }
}
