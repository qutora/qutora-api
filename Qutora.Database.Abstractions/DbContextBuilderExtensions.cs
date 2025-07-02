using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Qutora.Database.Abstractions;

/// <summary>
/// DbContext yapılandırma extension metodları
/// </summary>
public static class DbContextBuilderExtensions
{
    /// <summary>
    /// Qutora için DbContext ekler ve yapılandırır
    /// </summary>
    /// <typeparam name="TContext">DbContext tipi</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Konfigürasyon</param>
    /// <param name="dbProviderConfigKey">Sağlayıcı konfigürasyon anahtarı</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddQutoraDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string dbProviderConfigKey = "Database:Provider")
        where TContext : DbContext
    {
        services.AddSingleton<IDbProviderRegistry, DbProviderRegistry>();

        services.AddDbContext<TContext>((provider, options) =>
        {
            var registry = provider.GetRequiredService<IDbProviderRegistry>();
            var providerName = configuration[dbProviderConfigKey] ?? "SqlServer";
            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                   throw new InvalidOperationException("Connection string is not configured.");

            var dbProvider = registry.GetProvider(providerName);
            if (dbProvider == null)
                throw new InvalidOperationException($"Database provider '{providerName}' is not registered.");

            dbProvider.ConfigureDbContext(options, connectionString);
        });

        return services;
    }
}