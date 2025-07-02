using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Qutora.Database.Abstractions;

namespace Qutora.Database.MySQL;

/// <summary>
/// MySQL database provider
/// </summary>
public class MySqlProvider : IDbProvider
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName => "MySQL";

    /// <summary>
    /// DbContext configuration
    /// </summary>
    public void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure();
                mysqlOptions.MigrationsAssembly("Qutora.Database.MySQL");
            }
        );
    }

    /// <summary>
    /// Execution strategy for MySQL
    /// </summary>
    public IExecutionStrategy CreateExecutionStrategy(DbContext context)
    {
        return context.Database.CreateExecutionStrategy();
    }

    /// <summary>
    /// MySQL ordering expression
    /// </summary>
    public string GetOrderByExpression(string columnName, bool isAscending = true)
    {
        return $"`{columnName}` {(isAscending ? "ASC" : "DESC")}";
    }
}