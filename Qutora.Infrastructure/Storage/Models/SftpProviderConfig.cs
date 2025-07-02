using System.Text.Json;

namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// Configuration class for SFTP provider
/// </summary>
public class SftpProviderConfig : IProviderConfig
{
    /// <summary>
    /// Provider unique identifier
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider type
    /// </summary>
    public string ProviderType => "sftp";

    /// <summary>
    /// SFTP server address
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SFTP port number
    /// </summary>
    public int Port { get; set; } = 22;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Private key file path
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;

    /// <summary>
    /// Private key passphrase
    /// </summary>
    public string Passphrase { get; set; } = string.Empty;

    /// <summary>
    /// Root directory path
    /// </summary>
    public string RootPath { get; set; } = "/";

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
            return new SftpProviderConfig();

        try
        {
            return JsonSerializer.Deserialize<SftpProviderConfig>(json);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"SftpProviderConfig JSON parsing error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts to provider options
    /// </summary>
    public object ToOptions()
    {
        return new SftpProviderOptions
        {
            ProviderId = ProviderId,
            Host = Host,
            Port = Port,
            Username = Username,
            Password = Password,
            PrivateKeyPath = PrivateKeyPath,
            Passphrase = Passphrase,
            RootPath = RootPath
        };
    }
}
