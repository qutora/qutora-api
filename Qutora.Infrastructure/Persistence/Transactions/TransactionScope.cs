using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Qutora.Infrastructure.Persistence.Transactions;

/// <summary>
/// Class used for Ambient Transaction model.
/// Automatically tracks transaction state.
/// </summary>
public class TransactionScope : IDisposable
{
    private static readonly AsyncLocal<TransactionScope?> _current = new();
    private readonly IDbContextTransaction? _transaction;
    private readonly TransactionScope? _parent;
    private readonly DbContext _context;
    private bool _completed = false;

    /// <summary>
    /// Gets the current active transaction scope
    /// </summary>
    public static TransactionScope? Current => _current.Value;

    private TransactionScope(DbContext context, IDbContextTransaction? transaction)
    {
        _parent = Current;
        _transaction = transaction;
        _context = context;
        _current.Value = this;
    }

    /// <summary>
    /// Starts a new transaction scope.
    /// If a transaction already exists, does not start a new transaction.
    /// </summary>
    public static async Task<TransactionScope> BeginTransactionAsync(DbContext context,
        CancellationToken cancellationToken = default)
    {
        if (Current != null) return new TransactionScope(context, null);

        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new TransactionScope(context, transaction);
    }

    /// <summary>
    /// Completes the transaction
    /// </summary>
    public async Task CompleteAsync()
    {
        if (_transaction != null && !_completed)
        {
            await _context.SaveChangesAsync();
            await _transaction.CommitAsync();
            _completed = true;
        }
    }

    /// <summary>
    /// Disposes the transaction.
    /// If not completed, performs rollback.
    /// </summary>
    public void Dispose()
    {
        _current.Value = _parent;

        if (_transaction != null && !_completed)
            try
            {
                _transaction.Rollback();
            }
            finally
            {
                _transaction.Dispose();
            }
    }
}
