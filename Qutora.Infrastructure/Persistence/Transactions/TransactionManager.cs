using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;

namespace Qutora.Infrastructure.Persistence.Transactions;

/// <summary>
/// Class for centrally managing all transactions in the application
/// </summary>
public class TransactionManager : ITransactionManager
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TransactionManager> _logger;

    /// <summary>
    /// Creates a new TransactionManager instance
    /// </summary>
    public TransactionManager(ApplicationDbContext context, ILogger<TransactionManager> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the specified operation within a transaction and returns the result.
    /// If a transaction already exists, does not start a new transaction.
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            _logger.LogDebug("Using existing transaction");
            return await operation();
        }

        _logger.LogDebug("Starting new transaction");
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _logger.LogDebug("Transaction completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rolling back transaction");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Executes the specified operation within a transaction.
    /// If a transaction already exists, does not start a new transaction.
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    /// <summary>
    /// Checks if there is an active transaction
    /// </summary>
    public bool IsInTransaction()
    {
        return _context.Database.CurrentTransaction != null;
    }

    /// <summary>
    /// Starts a new transaction scope.
    /// If a transaction already exists, returns NoOpDisposable.
    /// </summary>
    public async Task<IDisposable> BeginTransactionScopeAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            _logger.LogDebug("Returning NoOpDisposable because existing transaction");
            return new NoOpDisposable();
        }

        _logger.LogDebug("Starting new transaction scope");
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new InternalTransactionScope(_context, transaction, _logger);
    }

    /// <summary>
    /// Custom TransactionScope created by TransactionManager
    /// </summary>
    private class InternalTransactionScope(
        ApplicationDbContext context,
        IDbContextTransaction transaction,
        ILogger logger)
        : IDisposable
    {
        public void Dispose()
        {
            try
            {
                logger.LogDebug("Closing transaction scope and saving changes");
                context.SaveChanges();
                transaction.Commit();
                logger.LogDebug("Transaction completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Rolling back transaction");
                transaction.Rollback();
                throw;
            }
            finally
            {
                transaction.Dispose();
            }
        }
    }
}
