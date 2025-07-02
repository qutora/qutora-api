namespace Qutora.Infrastructure.Interfaces.UnitOfWork;

/// <summary>
/// Interface for UnitOfWork factory
/// </summary>
public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Creates UnitOfWork for database provider
    /// </summary>
    /// <param name="providerName">Database provider name</param>
    /// <returns>UnitOfWork object</returns>
    IUnitOfWork Create(string providerName);
}