using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Qutora.Infrastructure.Security;

public static class DataProtectionWarningExtensions
{
    public static IServiceCollection AddDataProtectionWarning(
        this IServiceCollection services,
        string keysPath,
        KeySourceType keySourceType)
    {
        services.AddSingleton<IHostedService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<DataProtectionWarningService>>();
            var environment = provider.GetRequiredService<IHostEnvironment>();
            return new DataProtectionWarningService(logger, environment, keysPath, keySourceType);
        });
        
        return services;
    }
}