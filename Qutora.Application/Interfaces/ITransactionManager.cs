namespace Qutora.Application.Interfaces;

/// <summary>
/// Interface for centrally managing all transactions in the application
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// Executes the specified operation within a transaction and returns the result.
    /// If a transaction already exists, does not start a new transaction.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the specified operation within a transaction.
    /// If a transaction already exists, does not start a new transaction.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if there is an active transaction
    /// </summary>
    bool IsInTransaction();

    /// <summary>
    /// Starts a new transaction scope.
    /// If a transaction already exists, returns NoOpDisposable.
    /// </summary>
    Task<IDisposable> BeginTransactionScopeAsync(CancellationToken cancellationToken = default);
}
