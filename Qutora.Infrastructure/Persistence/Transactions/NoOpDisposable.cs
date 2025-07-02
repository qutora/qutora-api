namespace Qutora.Infrastructure.Persistence.Transactions;

/// <summary>
/// IDisposable implementation that performs no operations.
/// Used when a transaction is already active.
/// </summary>
public class NoOpDisposable : IDisposable
{
    /// <summary>
    /// Performs no operations
    /// </summary>
    public void Dispose()
    {
    }
}
