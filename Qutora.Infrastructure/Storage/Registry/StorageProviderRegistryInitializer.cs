using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qutora.Infrastructure.Interfaces.Storage;

namespace Qutora.Infrastructure.Storage.Registry;

/// <summary>
/// Service that ensures automatic initialization of StorageProviderTypeRegistry when the application starts.
/// </summary>
public class StorageProviderRegistryInitializer : IHostedService
{
    private readonly IStorageProviderTypeRegistry _registry;
    private readonly ILogger<StorageProviderRegistryInitializer> _logger;

    public StorageProviderRegistryInitializer(
        IStorageProviderTypeRegistry registry,
        ILogger<StorageProviderRegistryInitializer> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing: StorageProviderTypeRegistry");
        _registry.Initialize();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
