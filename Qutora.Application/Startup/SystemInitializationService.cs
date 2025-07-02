using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;

namespace Qutora.Application.Startup;

public class SystemInitializationService(
    IServiceProvider serviceProvider,
    ILogger<SystemInitializationService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("System initialization started");

        try
        {
            using var scope = serviceProvider.CreateScope();

            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var isInitialized = await authService.IsSystemInitializedAsync(cancellationToken);

            if (isInitialized)
            {
                logger.LogInformation("System is already initialized, performing post-initialization checks");

                var approvalSettingsService = scope.ServiceProvider.GetRequiredService<IApprovalSettingsService>();
                await approvalSettingsService.EnsureGlobalSystemPolicyExistsAsync(cancellationToken);
                logger.LogInformation("✅ Global System Policy verification completed");

                await EnsureStorageProvidersActiveAsync(scope, cancellationToken);

                await EnsureDefaultBucketsExistAsync(scope, cancellationToken);

                logger.LogInformation("✅ All post-initialization checks completed successfully");
            }
            else
            {
                logger.LogInformation("System not yet initialized, skipping post-initialization tasks");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during system initialization");
        }

        logger.LogInformation("System initialization completed");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("System initialization service stopped");
        return Task.CompletedTask;
    }

    private async Task EnsureStorageProvidersActiveAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var storageProviderService = scope.ServiceProvider.GetRequiredService<IStorageProviderService>();
            var providers = await storageProviderService.GetAllAsync(cancellationToken);

            var activeProviders = providers.Where(p => p.IsActive).ToList();
            if (!activeProviders.Any())
            {
                logger.LogWarning("⚠️ No active storage providers found");
                return;
            }

            logger.LogInformation("✅ Found {Count} active storage provider(s): {Providers}",
                activeProviders.Count,
                string.Join(", ", activeProviders.Select(p => p.Name)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking storage providers");
        }
    }

    private async Task EnsureDefaultBucketsExistAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        try
        {
            var bucketService = scope.ServiceProvider.GetRequiredService<IStorageBucketService>();
            var buckets = await bucketService.GetPaginatedBucketsAsync(1, 100);

            var defaultBucket = buckets.FirstOrDefault(b => b is { IsDefault: true, IsActive: true });
            if (defaultBucket == null)
            {
                logger.LogWarning("⚠️ No default storage bucket found");
                return;
            }

            logger.LogInformation("✅ Default storage bucket found: {BucketPath}", defaultBucket.Path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking default storage buckets");
        }
    }
}
