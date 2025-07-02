using System.Text.Json;

namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// Configuration class for FTP provider
/// </summary>
public class FtpProviderConfig : IProviderConfig
{
    /// <summary>
    /// Provider unique identifier
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider type
    /// </summary>
    public string ProviderType => "ftp";

    /// <summary>
    /// FTP server address
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// FTP port number
    /// </summary>
    public int Port { get; set; } = 21;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Root directory path
    /// </summary>
    public string RootPath { get; set; } = "/";

    /// <summary>
    /// Use SSL
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Use passive mode
    /// </summary>
    public bool UsePassiveMode { get; set; } = true;

    /// <summary>
    /// Additional connection options
    /// </summary>
    public string ConnectionOptions { get; set; } = string.Empty;

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
            return new FtpProviderConfig();

        try
        {
            return JsonSerializer.Deserialize<FtpProviderConfig>(json);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"FtpProviderConfig JSON parsing error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts to FtpProviderOptions object
    /// </summary>
    public object ToOptions()
    {
        return new FtpProviderOptions
        {
            ProviderId = ProviderId,
            Host = Host,
            Port = Port,
            Username = Username,
            Password = Password,
            RootDirectory = RootPath,
            UseSsl = UseSSL,
            UsePassiveMode = UsePassiveMode,
            ConnectionOptions = ConnectionOptions,
            UseSftp = false 
        };
    }
}
