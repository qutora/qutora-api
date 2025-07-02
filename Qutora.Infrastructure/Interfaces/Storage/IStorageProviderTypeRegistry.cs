using System.Reflection;

namespace Qutora.Infrastructure.Interfaces.Storage;

/// <summary>
/// Registry interface for managing storage provider types
/// </summary>
public interface IStorageProviderTypeRegistry
{
    /// <summary>
    /// Initializes the registry
    /// </summary>
    void Initialize();

    /// <summary>
    /// Scans a specific assembly and registers providers
    /// </summary>
    void RegisterProviderAssembly(Assembly assembly);

    /// <summary>
    /// Used to manually register provider types
    /// </summary>
    bool RegisterProviderType(string providerType, Type implementationType);

    /// <summary>
    /// Checks if the provider type is valid
    /// </summary>
    bool IsValidProviderType(string providerType);

    /// <summary>
    /// Returns the Type object for the provider type
    /// </summary>
    Type? GetProviderType(string providerType);

    /// <summary>
    /// Returns all registered provider types
    /// </summary>
    IEnumerable<string> GetAvailableProviderTypes();
}