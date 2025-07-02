using System.Text.Json;

namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// Configuration class for MinIO and S3 providers
/// </summary>
public class MinioProviderConfig : IProviderConfig
{
    /// <summary>
    /// Provider unique identifier
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider type
    /// </summary>
    public string ProviderType => "minio";

    /// <summary>
    /// Endpoint URL
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
    /// Region information
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// SSL usage
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Path style usage
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Default duration for temporary links (minutes)
    /// </summary>
    public int DefaultExpiry { get; set; } = 60;

    /// <summary>
    /// Converts config object to JSON
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Creates config object from JSON data
    /// </summary>
    public static IProviderConfig? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new MinioProviderConfig();

        try
        {
            return JsonSerializer.Deserialize<MinioProviderConfig>(json);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"MinioProviderConfig JSON parsing error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts to MinioProviderOptions object
    /// </summary>
    public object ToOptions()
    {
        return new MinioProviderOptions
        {
            ProviderId = ProviderId,
            Endpoint = Endpoint,
            AccessKey = AccessKey,
            SecretKey = SecretKey,
            BucketName = BucketName,
            Region = Region,
            UseSSL = UseSSL,
            ForcePathStyle = ForcePathStyle,
            DefaultExpiry = DefaultExpiry
        };
    }
}
