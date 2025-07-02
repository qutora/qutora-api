using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Qutora.Infrastructure.Security;

public class DataProtectionWarningService(
    ILogger<DataProtectionWarningService> logger,
    IHostEnvironment environment,
    string keysPath,
    KeySourceType keySourceType)
    : BackgroundService
{
    private readonly string _environment = environment.EnvironmentName;
    private readonly string _keysPath = keysPath;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for application to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (keySourceType == KeySourceType.InternalGeneration)
            {
                if (_environment == "Production")
                {
                    // Critical warnings every 5 minutes in production
                    logger.LogCritical("*** CRITICAL PRODUCTION ALERT ***");
                    logger.LogCritical("** Data Protection keys are being generated INTERNALLY!");
                    logger.LogCritical("** Container restart will cause PERMANENT DATA LOSS!");
                    logger.LogCritical("** Action Required: Mount persistent volume to /app/keys");
                    logger.LogCritical("** Command: docker run -v qutora_keys:/app/keys qutora/qutora-api");
                    logger.LogCritical("*************************************************");
                    
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                else
                {
                    // Less frequent warnings in development
                    logger.LogWarning("** Development: Using internal key generation. Data will be lost on container restart.");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
            else
            {
                // No warnings needed for external keys, but log status
                logger.LogInformation("OK: Data Protection: Using secure external key storage ({KeySource})", keySourceType);
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}

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