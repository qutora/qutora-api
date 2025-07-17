namespace Qutora.Application.Interfaces.Storage;

/// <summary>
/// Interface for managing storage providers
/// </summary>
public interface IStorageManager
{
    /// <summary>
    /// Returns the provider with the specified ID
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <returns>Storage provider</returns>
    Task<IStorageProvider> GetProviderAsync(string id);

    /// <summary>
    /// Returns the default provider
    /// </summary>
    /// <returns>Default storage provider</returns>
    Task<IStorageProvider> GetDefaultProviderAsync();

    /// <summary>
    /// Returns all registered provider IDs
    /// </summary>
    /// <returns>List of provider IDs</returns>
    Task<IEnumerable<string>> GetAvailableProviderNamesAsync();

    /// <summary>
    /// Reloads providers from database
    /// </summary>
    Task ReloadProvidersAsync();

    /// <summary>
    /// Tests provider connection
    /// </summary>
    /// <param name="providerType">Provider type</param>
    /// <param name="configJson">Provider configuration</param>
    /// <returns>Test result</returns>
    Task<(bool success, string message)> TestProviderConnectionAsync(string providerType, string configJson);

    /// <summary>
    /// Removes a specific provider from cache (for status changes)
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    Task RemoveProviderFromCacheAsync(string providerId);
}