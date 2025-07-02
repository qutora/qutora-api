using Qutora.Infrastructure.Storage.Models;

namespace Qutora.Infrastructure.Storage.Registry;

/// <summary>
/// Storage provider creator interface.
/// Interface that provider creators must implement for the Factory pattern.
/// </summary>
public interface IProviderCreator
{
    /// <summary>
    /// Provider type identifier.
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Checks whether this creator can handle the specified provider type.
    /// </summary>
    /// <param name="providerType">Provider type.</param>
    /// <returns>True if the provider type can be handled, false otherwise.</returns>
    bool CanHandle(string providerType);

    /// <summary>
    /// Creates a storage provider object according to the specified configuration.
    /// </summary>
    /// <param name="config">Provider configuration.</param>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    /// <returns>Created storage provider object.</returns>
    IStorageProviderAdapter Create(IProviderConfig config, IServiceProvider serviceProvider);
}
