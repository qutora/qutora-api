using Microsoft.Extensions.Logging;
using System.Reflection;
using Qutora.Infrastructure.Interfaces.Storage;

namespace Qutora.Infrastructure.Storage.Registry;

/// <summary>
/// Registry class that manages storage provider types.
/// </summary>
public class StorageProviderTypeRegistry : IStorageProviderTypeRegistry
{
    private readonly Dictionary<string, Type> _providerTypes = new();
    private readonly object _lock = new();
    private bool _isInitialized = false;
    private readonly ILogger<StorageProviderTypeRegistry> _logger;

    public StorageProviderTypeRegistry(ILogger<StorageProviderTypeRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Scans basic assemblies and registers provider types
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;

        lock (_lock)
        {
            if (_isInitialized) return;

            ScanAssembliesForProviders(AppDomain.CurrentDomain.GetAssemblies());

            _isInitialized = true;

            _logger.LogInformation("Storage provider type registry initialized with {Count} provider types",
                _providerTypes.Count);
        }
    }

    /// <summary>
    /// Scans a specific assembly and registers providers
    /// </summary>
    public void RegisterProviderAssembly(Assembly assembly)
    {
        lock (_lock)
        {
            ScanAssembliesForProviders([assembly]);
        }
    }

    /// <summary>
    /// Used to manually register provider types
    /// </summary>
    public bool RegisterProviderType(string providerType, Type implementationType)
    {
        if (string.IsNullOrEmpty(providerType) || implementationType == null)
            return false;

        if (!typeof(IStorageProvider).IsAssignableFrom(implementationType))
        {
            _logger.LogWarning("Type {ImplementationType} does not implement IStorageProvider",
                implementationType.Name);
            return false;
        }

        lock (_lock)
        {
            if (_providerTypes.ContainsKey(providerType))
            {
                _logger.LogWarning("Provider type {ProviderType} is already registered", providerType);
                return false;
            }

            _providerTypes.Add(providerType, implementationType);
            _logger.LogInformation("Storage provider manually registered: {ProviderType} -> {ProviderClass}",
                providerType, implementationType.Name);
            return true;
        }
    }

    /// <summary>
    /// Scans assembly collection to find IStorageProvider implementations
    /// </summary>
    private void ScanAssembliesForProviders(IEnumerable<Assembly> assemblies)
    {
        var providerTypes = assemblies
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return [];
                }
            })
            .Where(t => t != typeof(IStorageProvider) &&
                        typeof(IStorageProvider).IsAssignableFrom(t) &&
                        t is { IsInterface: false, IsAbstract: false })
            .ToList();

        foreach (var type in providerTypes)
            try
            {
                var attributes = type.GetCustomAttributes(typeof(ProviderTypeAttribute), false);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ProviderTypeAttribute;
                    if (attribute != null && !string.IsNullOrEmpty(attribute.ProviderType))
                    {
                        _providerTypes.Add(attribute.ProviderType, type);
                        _logger.LogInformation(
                            "Storage provider registered via attribute: {ProviderType} -> {ProviderClass}",
                            attribute.ProviderType, type.Name);
                        continue;
                    }
                }

                var providerTypeProperty = type.GetProperty("ProviderType", BindingFlags.Public | BindingFlags.Static);
                if (providerTypeProperty != null && providerTypeProperty.PropertyType == typeof(string))
                {
                    var providerType = providerTypeProperty.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(providerType))
                    {
                        _providerTypes.Add(providerType, type);
                        _logger.LogInformation(
                            "Storage provider registered via static property: {ProviderType} -> {ProviderClass}",
                            providerType, type.Name);
                        continue;
                    }
                }

                var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
                if (defaultConstructor != null)
                    try
                    {
                        var tempInstance = Activator.CreateInstance(type) as IStorageProvider;
                        if (tempInstance != null)
                        {
                            var providerType = tempInstance.ProviderType;

                            if (!string.IsNullOrEmpty(providerType) && !_providerTypes.ContainsKey(providerType))
                            {
                                _providerTypes.Add(providerType, type);
                                _logger.LogInformation(
                                    "Storage provider auto-registered: {ProviderType} -> {ProviderClass}",
                                    providerType, type.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error creating instance of provider type {ProviderType}", type.Name);
                    }
                else
                    _logger.LogWarning(
                        "Storage provider {ProviderClass} does not have a parameterless constructor or the required attributes",
                        type.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error initializing provider type {ProviderType}", type.Name);
            }
    }

    /// <summary>
    /// Checks if the provider type is valid
    /// </summary>
    public bool IsValidProviderType(string providerType)
    {
        return _providerTypes.ContainsKey(providerType);
    }

    /// <summary>
    /// Returns the Type object for the provider type
    /// </summary>
    public Type? GetProviderType(string providerType)
    {
        return _providerTypes.TryGetValue(providerType, out var type) ? type : null;
    }

    /// <summary>
    /// Returns all registered provider types
    /// </summary>
    public IEnumerable<string> GetAvailableProviderTypes()
    {
        return _providerTypes.Keys.ToList();
    }
}
