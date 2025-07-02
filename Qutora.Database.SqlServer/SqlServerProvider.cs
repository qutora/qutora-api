using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qutora.Database.Abstractions;

namespace Qutora.Database.SqlServer;

/// <summary>
/// SQL Server database provider
/// </summary>
public class SqlServerProvider : IDbProvider
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName => "SqlServer";

    /// <summary>
    /// DbContext configuration
    /// </summary>
    public void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
            sqlOptions.MigrationsAssembly("Qutora.Database.SqlServer");
        });
    }

    /// <summary>
    /// Execution strategy for SQL Server
    /// </summary>
    public IExecutionStrategy CreateExecutionStrategy(DbContext context)
    {
        return new SqlServerRetryingExecutionStrategy(context);
    }

    /// <summary>
    /// SQL Server ordering expression
    /// </summary>
    public string GetOrderByExpression(string columnName, bool isAscending = true)
    {
        return $"{columnName} {(isAscending ? "ASC" : "DESC")}";
    }
}