using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qutora.Database.Abstractions;

namespace Qutora.Database.PostgreSQL;

/// <summary>
/// PostgreSQL database provider
/// </summary>
public class PostgreSqlProvider : IDbProvider
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName => "PostgreSQL";

    /// <summary>
    /// DbContext configuration
    /// </summary>
    public void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure();
            npgsqlOptions.MigrationsAssembly("Qutora.Database.PostgreSQL");
        });
    }

    /// <summary>
    /// Execution strategy for PostgreSQL
    /// </summary>
    public IExecutionStrategy CreateExecutionStrategy(DbContext context)
    {
        return context.Database.CreateExecutionStrategy();
    }

    /// <summary>
    /// PostgreSQL ordering expression
    /// </summary>
    public string GetOrderByExpression(string columnName, bool isAscending = true)
    {
        // In PostgreSQL, column names are case sensitive, so double quotes are used.
        return $"\"{columnName}\" {(isAscending ? "ASC" : "DESC")}";
    }
}