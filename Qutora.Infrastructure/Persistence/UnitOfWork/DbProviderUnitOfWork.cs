using System.Net.Mime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Database.Abstractions;
using Qutora.Infrastructure.Persistence.Repositories;
using Qutora.Infrastructure.Persistence.Transactions;
using Qutora.Shared.Exceptions;

namespace Qutora.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// DbProvider-specific UnitOfWork implementation
/// </summary>
public sealed class DbProviderUnitOfWork(
    ApplicationDbContext context,
    IDbProvider dbProvider,
    ILoggerFactory loggerFactory,
    ITransactionManager transactionManager)
    : IDbProviderUnitOfWork
{
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly IDbProvider _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));

    private readonly ILoggerFactory _loggerFactory =
        loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    private readonly ITransactionManager _transactionManager =
        transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));

    private IDbContextTransaction? _transaction;
    private bool _disposed;

    private IDocumentRepository? _documentRepository;
    private ICategoryRepository? _categoryRepository;
    private IStorageProviderRepository? _storageProviderRepository;
    private IApiKeyRepository? _apiKeyRepository;
    private IMetadataRepository? _metadataRepository;
    private IMetadataSchemaRepository? _metadataSchemaRepository;
    private IDocumentVersionRepository? _documentVersionRepository;
    private IAuditLogRepository? _auditLogRepository;
    private IStorageBucketRepository? _storageBucketRepository;
    private IBucketPermissionRepository? _bucketPermissionRepository;
    private IApiKeyBucketPermissionRepository? _apiKeyBucketPermissionRepository;
    private IDocumentShareRepository? _documentShareRepository;
    private IDocumentShareViewRepository? _documentShareViewRepository;
    private IApprovalSettingsRepository? _approvalSettingsRepository;
    private IApprovalPolicyRepository? _approvalPolicyRepository;
    private IShareApprovalRequestRepository? _shareApprovalRequestRepository;
    private IApprovalDecisionRepository? _approvalDecisionRepository;
    private IApprovalHistoryRepository? _approvalHistoryRepository;
    private IEmailSettingsRepository? _emailSettingsRepository;
    private IEmailTemplateRepository? _emailTemplateRepository;
    private IRefreshTokenRepository? _refreshTokenRepository;
    private ISystemSettingsRepository? _systemSettingsRepository;

    public IDbProvider DbProvider => _dbProvider;

    public IDocumentRepository Documents => _documentRepository ??=
        new DocumentRepository(_context, _loggerFactory.CreateLogger<DocumentRepository>());

    public ICategoryRepository Categories => _categoryRepository ??=
        new CategoryRepository(_context, _loggerFactory.CreateLogger<CategoryRepository>());

    public IStorageProviderRepository StorageProviders => _storageProviderRepository ??=
        new StorageProviderRepository(_context, _loggerFactory.CreateLogger<StorageProviderRepository>());

    public IApiKeyRepository ApiKeys => _apiKeyRepository ??=
        new ApiKeyRepository(_context, _loggerFactory.CreateLogger<ApiKeyRepository>());

    public IMetadataRepository Metadata => _metadataRepository ??=
        new MetadataRepository(_context, _loggerFactory.CreateLogger<MetadataRepository>());

    public IMetadataSchemaRepository MetadataSchemas => _metadataSchemaRepository ??=
        new MetadataSchemaRepository(_context, _loggerFactory.CreateLogger<MetadataSchemaRepository>());

    public IDocumentVersionRepository DocumentVersions => _documentVersionRepository ??=
        new DocumentVersionRepository(_context, _loggerFactory.CreateLogger<DocumentVersionRepository>());

    public IAuditLogRepository AuditLogs => _auditLogRepository ??=
        new AuditLogRepository(_context, _loggerFactory.CreateLogger<AuditLogRepository>());

    public IStorageBucketRepository StorageBuckets => _storageBucketRepository ??=
        new StorageBucketRepository(_context, _loggerFactory.CreateLogger<StorageBucketRepository>());

    public IBucketPermissionRepository BucketPermissions => _bucketPermissionRepository ??=
        new BucketPermissionRepository(_context, _loggerFactory.CreateLogger<BucketPermissionRepository>());

    public IApiKeyBucketPermissionRepository ApiKeyBucketPermissions => _apiKeyBucketPermissionRepository ??=
        new ApiKeyBucketPermissionRepository(_context, _loggerFactory.CreateLogger<ApiKeyBucketPermissionRepository>());

    public IDocumentShareRepository DocumentShares => _documentShareRepository ??=
        new DocumentShareRepository(_context, _loggerFactory.CreateLogger<DocumentShareRepository>());

    public IDocumentShareViewRepository DocumentShareViews => _documentShareViewRepository ??=
        new DocumentShareViewRepository(_context, _loggerFactory.CreateLogger<DocumentShareViewRepository>());

    public IApprovalSettingsRepository ApprovalSettings => _approvalSettingsRepository ??=
        new ApprovalSettingsRepository(_context, _loggerFactory.CreateLogger<ApprovalSettingsRepository>());

    public IApprovalPolicyRepository ApprovalPolicies => _approvalPolicyRepository ??=
        new ApprovalPolicyRepository(_context, _loggerFactory.CreateLogger<ApprovalPolicyRepository>());

    public IShareApprovalRequestRepository ShareApprovalRequests => _shareApprovalRequestRepository ??=
        new ShareApprovalRequestRepository(_context, _loggerFactory.CreateLogger<ShareApprovalRequestRepository>());

    public IApprovalDecisionRepository ApprovalDecisions => _approvalDecisionRepository ??=
        new ApprovalDecisionRepository(_context, _loggerFactory.CreateLogger<ApprovalDecisionRepository>());

    public IApprovalHistoryRepository ApprovalHistories => _approvalHistoryRepository ??=
        new ApprovalHistoryRepository(_context, _loggerFactory.CreateLogger<ApprovalHistoryRepository>());

    public IEmailSettingsRepository EmailSettings => _emailSettingsRepository ??=
        new EmailSettingsRepository(_context, _loggerFactory.CreateLogger<EmailSettingsRepository>());

    public IEmailTemplateRepository EmailTemplates => _emailTemplateRepository ??=
        new EmailTemplateRepository(_context, _loggerFactory.CreateLogger<EmailTemplateRepository>());

    public IRefreshTokenRepository RefreshTokens => _refreshTokenRepository ??=
        new RefreshTokenRepository(_context,_loggerFactory.CreateLogger<RefreshTokenRepository>());

    public ISystemSettingsRepository SystemSettings => _systemSettingsRepository ??=
        new SystemSettingsRepository(_context,_loggerFactory.CreateLogger<SystemSettingsRepository>());


    /// <summary>
    /// Returns the database execution strategy for use in transaction handling
    /// </summary>
    public IExecutionStrategy CreateExecutionStrategy()
    {
        return _dbProvider.CreateExecutionStrategy(_context);
    }

    /// <summary>
    /// Executes a function within a retry-enabled transaction scope
    /// </summary>
    public async Task ExecuteTransactionalAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await _transactionManager.ExecuteInTransactionAsync(operation, cancellationToken);
    }

    /// <summary>
    /// Executes a function within a retry-enabled transaction scope with a return value
    /// </summary>
    public async Task<TResult> ExecuteTransactionalAsync<TResult>(Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        return await _transactionManager.ExecuteInTransactionAsync(operation, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var strategy = CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => await _context.SaveChangesAsync(cancellationToken));
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("Transaction has not been started. Call BeginTransactionAsync first.");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null) return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Detects concurrency conflicts and throws error
    /// </summary>
    public async Task<(bool HasConflict, TEntity? DatabaseEntity, TEntity? CurrentEntity)>
        DetectConcurrencyConflictAsync<TEntity>(
            TEntity entity,
            CancellationToken cancellationToken = default) where TEntity : class
    {
        var strategy = CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return (false, null, null);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();

                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);

                if (databaseValues == null) return (true, null, entity);

                var databaseEntity = (TEntity)databaseValues.ToObject();

                return (true, databaseEntity, entity);
            }
        });
    }

    /// <summary>
    /// Legacy method - kept for compatibility, now always throws concurrency error
    /// </summary>
    public async Task<bool> TryResolveConcurrencyConflictAsync<TEntity>(
        TEntity entity,
        Func<Task<TEntity>> getUpdatedEntity,
        Func<TEntity, TEntity, Task<TEntity>> handleConflict,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var strategy = CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyException(
                    "Record has been modified by another user. Please refresh the page and reapply your changes.",
                    ex);
            }
        });
    }

    /// <summary>
    /// Checks if there is an active transaction
    /// </summary>
    public bool IsInTransaction()
    {
        return _transactionManager.IsInTransaction();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }
}
