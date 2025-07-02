namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// Base interface for all provider config classes
/// </summary>
public interface IProviderConfig
{
    /// <summary>
    /// Provider unique identifier
    /// </summary>
    string ProviderId { get; set; }

    /// <summary>
    /// Provider type (must be lowercase: filesystem, minio, ftp, sftp)
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Converts config object to JSON
    /// </summary>
    string ToJson();

    /// <summary>
    /// Converts to Provider Options object
    /// </summary>
    object ToOptions();
}
