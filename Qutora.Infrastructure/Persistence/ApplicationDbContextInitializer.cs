using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Qutora.Database.Abstractions;

namespace Qutora.Infrastructure.Persistence;

/// <summary>
/// Performs database initialization operations
/// </summary>
public class ApplicationDbContextInitializer(
    ILogger<ApplicationDbContextInitializer> logger,
    ApplicationDbContext context,
    IDbProviderRegistry dbProviderRegistry,
    string name = "SqlServer")
{
    /// <summary>
    /// Initializes the database
    /// </summary>
    public async Task InitializeAsync()
    {
        // Retry logic for database connection issues (especially for Docker)
        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(5);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Database initialization attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
                
                var provider = dbProviderRegistry.GetProvider(name);
                if (provider == null)
                    logger.LogWarning(
                        "Database provider not found: {ProviderName}, using SqlServer as default",
                        name);

                // Use a longer timeout for database operations
                var originalTimeout = context.Database.GetCommandTimeout();
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

                try
                {
                    // Test database connection first
                    logger.LogInformation("Testing database connection...");
                    await context.Database.OpenConnectionAsync();
                    await context.Database.CloseConnectionAsync();
                    logger.LogInformation("✅ Database connection successful");
                    
                    // Check migration status
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                    
                    logger.LogInformation("Applied migrations: {AppliedCount}, Pending migrations: {PendingCount}", 
                        appliedMigrations.Count(), pendingMigrations.Count());

                    if (context.Database.IsSqlServer())
                    {
                        await InitializeDatabaseWithFallback("SQL Server");
                    }
                    else if (IsPostgreSql(context.Database))
                    {
                        await InitializeDatabaseWithFallback("PostgreSQL");
                    }
                    else if (IsMySql(context.Database))
                    {
                        await InitializeDatabaseWithFallback("MySQL");
                    }
                    else
                    {
                        logger.LogWarning("Unknown database provider, using EnsureCreated");
                        await context.Database.EnsureCreatedAsync();
                    }

                    context.EnsureSeedDirectoriesCreated();
                    logger.LogInformation("✅ Database successfully initialized: {ProviderName}", name);
                    return; // Success, exit retry loop
                }
                finally
                {
                    // Restore original timeout
                    context.Database.SetCommandTimeout(originalTimeout);
                }
            }
            catch (Exception ex) when (attempt < maxRetries && IsConnectionException(ex))
            {
                logger.LogWarning(ex, "Database connection failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay} seconds...", 
                    attempt, maxRetries, retryDelay.TotalSeconds);
                
                await Task.Delay(retryDelay);
                retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 1.5, 30)); // Exponential backoff
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during database initialization on attempt {Attempt}/{MaxRetries}.", attempt, maxRetries);
                
                if (attempt == maxRetries)
                {
                    logger.LogError("All {MaxRetries} database initialization attempts failed", maxRetries);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Determines if an exception is a connection-related exception that should be retried
    /// </summary>
    private bool IsConnectionException(Exception ex)
    {
        var exceptionMessage = ex.Message.ToLower();
        var innerExceptionMessage = ex.InnerException?.Message?.ToLower() ?? "";
        
        // Common connection-related error patterns
        var connectionErrors = new[]
        {
            "connection refused",
            "connection timeout",
            "timeout expired",
            "network is unreachable",
            "host is unreachable",
            "connection reset",
            "connection failed",
            "unable to connect",
            "server is not ready",
            "database is starting up"
        };
        
        return connectionErrors.Any(error => 
            exceptionMessage.Contains(error) || innerExceptionMessage.Contains(error));
    }

    /// <summary>
    /// Attempts migration first, falls back to EnsureCreated if migrations fail
    /// </summary>
    private async Task InitializeDatabaseWithFallback(string providerName)
    {
        try
        {
            // First, try to use migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations for {Provider}", 
                    pendingMigrations.Count(), providerName);
                
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Migrations applied successfully for {Provider}", providerName);
            }
            else
            {
                // Check if database exists and has tables
                var hasSystemSettings = await CheckIfTablesExist();
                
                if (!hasSystemSettings)
                {
                    // Try to get all migrations to see if any exist
                    var allMigrations = context.Database.GetMigrations();
                    
                    if (allMigrations.Any())
                    {
                        logger.LogInformation("Applying {Count} available migrations for {Provider}", 
                            allMigrations.Count(), providerName);
                        await context.Database.MigrateAsync();
                        logger.LogInformation("✅ Migrations applied successfully for {Provider}", providerName);
                    }
                    else
                    {
                        logger.LogWarning("No migrations found, using EnsureCreated as fallback");
                        var created = await context.Database.EnsureCreatedAsync();
                        if (created)
                        {
                            logger.LogInformation("✅ Database created using EnsureCreated for {Provider}", providerName);
                        }
                    }
                }
                else
                {
                    logger.LogInformation("✅ Database already initialized for {Provider}", providerName);
                }
            }
        }
        catch (Exception migrationEx)
        {
            logger.LogWarning(migrationEx, "Migration failed for {Provider}, trying EnsureCreated fallback", providerName);
            
            try
            {
                // Fallback to EnsureCreated
                var created = await context.Database.EnsureCreatedAsync();
                if (created)
                {
                    logger.LogInformation("✅ Database created successfully using EnsureCreated for {Provider}", providerName);
                }
                else
                {
                    logger.LogInformation("✅ Database already exists for {Provider}", providerName);
                }
            }
            catch (Exception fallbackEx)
            {
                logger.LogError(fallbackEx, "Both migration and EnsureCreated failed for {Provider}", providerName);
                throw;
            }
        }
    }

    /// <summary>
    /// Checks if essential tables exist in the database
    /// </summary>
    private async Task<bool> CheckIfTablesExist()
    {
        try
        {
            // Try to query SystemSettings table to check if database is initialized
            await context.SystemSettings.CountAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the database is PostgreSQL
    /// </summary>
    private bool IsPostgreSql(DatabaseFacade database)
    {
        var providerName = database.ProviderName;
        return providerName != null &&
               (providerName.Contains("Npgsql") ||
                name.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the database is MySQL
    /// </summary>
    private bool IsMySql(DatabaseFacade database)
    {
        var providerName = database.ProviderName;
        return providerName != null &&
               (providerName.Contains("MySql") ||
                name.Equals("MySQL", StringComparison.OrdinalIgnoreCase));
    }
}
