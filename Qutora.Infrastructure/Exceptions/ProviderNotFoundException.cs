namespace Qutora.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a storage provider is not found
/// </summary>
public class ProviderNotFoundException : Exception
{
    /// <summary>
    /// ID of the provider that was not found
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Creates a new instance of the exception with the specified provider ID
    /// </summary>
    /// <param name="providerId">ID of the provider that was not found</param>
    public ProviderNotFoundException(string providerId)
        : base($"Storage provider not found: {providerId}")
    {
        ProviderId = providerId;
    }

    /// <summary>
    /// Creates a new instance of the exception with the specified provider ID and inner exception
    /// </summary>
    /// <param name="providerId">ID of the provider that was not found</param>
    /// <param name="innerException">Inner exception</param>
    public ProviderNotFoundException(string providerId, Exception innerException)
        : base($"Storage provider not found: {providerId}", innerException)
    {
        ProviderId = providerId;
    }
}