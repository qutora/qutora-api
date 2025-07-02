namespace Qutora.Infrastructure.Storage.Exceptions;

/// <summary>
/// Exception thrown when the requested storage provider is not found.
/// </summary>
public class ProviderNotFoundException : Exception
{
    /// <summary>
    /// Name of the provider that was not found
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Creates a new ProviderNotFoundException.
    /// </summary>
    /// <param name="providerName">Name of the provider that was not found</param>
    public ProviderNotFoundException(string providerName)
        : base($"Provider '{providerName}' not found. Make sure it is defined in the database and is active.")
    {
        ProviderName = providerName;
    }

    /// <summary>
    /// Creates a new ProviderNotFoundException.
    /// </summary>
    /// <param name="providerName">Name of the provider that was not found</param>
    /// <param name="message">Error message</param>
    public ProviderNotFoundException(string providerName, string message)
        : base(message)
    {
        ProviderName = providerName;
    }

    /// <summary>
    /// Creates a new ProviderNotFoundException.
    /// </summary>
    /// <param name="providerName">Name of the provider that was not found</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ProviderNotFoundException(string providerName, string message, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = providerName;
    }
}
