using Qutora.Database.Abstractions;

namespace Qutora.Application.Interfaces.UnitOfWork;

/// <summary>
/// UnitOfWork interface integrated with database provider
/// </summary>
public interface IDbProviderUnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Database provider being used
    /// </summary>
    IDbProvider DbProvider { get; }

    /// <summary>
    /// Executes operation using transaction management method that can handle nested transactions
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    new Task<TResult> ExecuteTransactionalAsync<TResult>(Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes operation using transaction management method that can handle nested transactions
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task returned when operation completes</returns>
    new Task ExecuteTransactionalAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}