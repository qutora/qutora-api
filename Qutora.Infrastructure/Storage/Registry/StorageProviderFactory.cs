using Microsoft.Extensions.Logging;
using Qutora.Infrastructure.Interfaces.Storage;
using Qutora.Infrastructure.Storage.Models;

namespace Qutora.Infrastructure.Storage.Registry;

/// <summary>
/// Factory class that creates Storage Provider objects.
/// </summary>
public class StorageProviderFactory
{
    private readonly IEnumerable<IProviderCreator> _creators;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StorageProviderFactory> _logger;
    private readonly IStorageProviderTypeRegistry _registry;

    public StorageProviderFactory(
        IEnumerable<IProviderCreator> creators,
        IServiceProvider serviceProvider,
        ILogger<StorageProviderFactory> logger,
        IStorageProviderTypeRegistry registry)
    {
        _creators = creators ?? throw new ArgumentNullException(nameof(creators));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Creates a provider from IProviderConfig object
    /// </summary>
    public IStorageProviderAdapter Create(IProviderConfig? config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var providerType = config.ProviderType.ToLowerInvariant();

        var creator = _creators.FirstOrDefault(c => c.CanHandle(providerType));

        if (creator != null)
        {
            _logger.LogDebug("Using creator for '{Provider}' type: {CreatorType}",
                providerType, creator.GetType().Name);
            return creator.Create(config, _serviceProvider);
        }

        var providerClass = _registry.GetProviderType(providerType);
        if (providerClass != null)
        {
            _logger.LogDebug("Using registry for '{Provider}' type, class: {ProviderClass}",
                providerType, providerClass.Name);

            try
            {
                var constructors = providerClass.GetConstructors();
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    var arguments = new object[parameters.Length];
                    var canResolve = true;

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];

                        if (param.ParameterType.Name.EndsWith("ProviderOptions"))
                        {
                            var optionsMethod = config.GetType().GetMethod("ToOptions");
                            if (optionsMethod != null)
                            {
                                arguments[i] = optionsMethod.Invoke(config, null);
                                continue;
                            }
                        }

                        var service = _serviceProvider.GetService(param.ParameterType);
                        if (service != null)
                        {
                            arguments[i] = service;
                        }
                        else
                        {
                            canResolve = false;
                            break;
                        }
                    }

                    if (canResolve) return (IStorageProviderAdapter)constructor.Invoke(arguments);
                }

                throw new InvalidOperationException($"No suitable constructor found for '{providerClass.Name}' class.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating provider of '{Provider}' type", providerType);
                throw new InvalidOperationException($"Cannot create provider of type {providerType}: {ex.Message}",
                    ex);
            }
        }

        throw new ArgumentException($"Unsupported provider type: {config.ProviderType}", nameof(config));
    }
}
