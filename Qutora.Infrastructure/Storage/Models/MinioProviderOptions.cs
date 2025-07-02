namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// MinIO storage provider configuration settings
/// </summary>
public class MinioProviderOptions
{
    /// <summary>
    /// Unique identifier for provider
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// MinIO server address (host:port)
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Access key
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret key
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Bucket name
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Use SSL
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Region information
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Use path style (usually true for MinIO)
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Default duration for temporary links (minutes)
    /// </summary>
    public int DefaultExpiry { get; set; } = 60;
}
