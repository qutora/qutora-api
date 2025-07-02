namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// File system storage provider configuration settings
/// </summary>
public class FileSystemProviderOptions
{
    /// <summary>
    /// Unique identifier for the provider
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Root directory where files will be stored
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether to store metadata files (content-type, etc.)
    /// </summary>
    public bool StoreMetadata { get; set; } = true;

    /// <summary>
    /// Maximum file size (bytes) (0 = unlimited)
    /// </summary>
    public long MaxFileSize { get; set; } = 0;

    /// <summary>
    /// Base directory path where files will be stored
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// Automatically create directory if it doesn't exist?
    /// </summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;
}
