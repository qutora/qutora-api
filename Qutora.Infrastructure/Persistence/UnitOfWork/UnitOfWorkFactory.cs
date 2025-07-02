using Microsoft.Extensions.Logging;
using Qutora.Database.Abstractions;
using Qutora.Infrastructure.Interfaces.UnitOfWork;
using Qutora.Infrastructure.Persistence.Transactions;

namespace Qutora.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// UnitOfWork factory implementation
/// </summary>
public class UnitOfWorkFactory(
    IServiceProvider serviceProvider,
    ApplicationDbContext dbContext,
    IDbProviderRegistry dbProviderRegistry,
    ILoggerFactory loggerFactory,
    ITransactionManager transactionManager)
    : IUnitOfWorkFactory
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ApplicationDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    private readonly IDbProviderRegistry _dbProviderRegistry =
        dbProviderRegistry ?? throw new ArgumentNullException(nameof(dbProviderRegistry));

    private readonly ILoggerFactory _loggerFactory =
        loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    private readonly ITransactionManager _transactionManager =
        transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));

    /// <summary>
    /// Creates UnitOfWork for the specified provider
    /// </summary>
    /// <param name="providerName">Database provider name</param>
    /// <returns>UnitOfWork object</returns>
    public IUnitOfWork Create(string providerName)
    {
        var dbProvider = _dbProviderRegistry.GetProvider(providerName);

        if (dbProvider == null)
            throw new InvalidOperationException($"Database provider '{providerName}' is not registered.");

        return new DbProviderUnitOfWork(_dbContext, dbProvider, _loggerFactory, _transactionManager);
    }
}
