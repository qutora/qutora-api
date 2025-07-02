using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Qutora.Database.Abstractions;

/// <summary>
/// Common interface for different database providers
/// </summary>
public interface IDbProvider
{
    /// <summary>
    /// Provider name (SqlServer, PostgreSQL, MySQL, etc.)
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Configures DbContext options
    /// </summary>
    /// <param name="options">DbContext options</param>
    /// <param name="connectionString">Connection string</param>
    void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString);

    /// <summary>
    /// Creates provider-specific execution strategy
    /// </summary>
    /// <param name="context">DbContext</param>
    /// <returns>Execution Strategy</returns>
    IExecutionStrategy CreateExecutionStrategy(DbContext context);

    /// <summary>
    /// Creates ordering clause based on database provider
    /// </summary>
    /// <param name="columnName">Name of column to sort by</param>
    /// <param name="isAscending">Is ascending sort?</param>
    /// <returns>Ordering clause appropriate for database provider</returns>
    string GetOrderByExpression(string columnName, bool isAscending = true);
}