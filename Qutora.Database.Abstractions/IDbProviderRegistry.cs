namespace Qutora.Database.Abstractions;

/// <summary>
/// Registry for managing database providers
/// </summary>
public interface IDbProviderRegistry
{
    /// <summary>
    /// Registers a database provider
    /// </summary>
    /// <param name="provider">Provider</param>
    void RegisterProvider(IDbProvider provider);

    /// <summary>
    /// Gets provider by name
    /// </summary>
    /// <param name="providerName">Provider name</param>
    /// <returns>Provider implementation or null</returns>
    IDbProvider? GetProvider(string providerName);

    /// <summary>
    /// Returns all registered provider names
    /// </summary>
    /// <returns>Provider names</returns>
    IEnumerable<string> GetAvailableProviders();

    /// <summary>
    /// Extracts DbContext provider name
    /// </summary>
    /// <param name="providerName">EF Core Provider name</param>
    /// <returns>Provider name</returns>
    string ExtractProviderName(string? providerName);
}