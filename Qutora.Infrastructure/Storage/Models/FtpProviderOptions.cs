namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// FTP/SFTP storage provider configuration settings
/// </summary>
public class FtpProviderOptions
{
    /// <summary>
    /// Unique identifier for provider
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// FTP server address
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Port number (21 for FTP, 22 for SFTP)
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
    /// Use SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Whether to use SFTP protocol
    /// </summary>
    public bool UseSftp { get; set; } = false;

    /// <summary>
    /// Root directory
    /// </summary>
    public string RootDirectory { get; set; } = "/";

    /// <summary>
    /// Root directory (legacy name for compatibility)
    /// </summary>
    public string RootPath
    {
        get => RootDirectory;
        set => RootDirectory = value;
    }

    /// <summary>
    /// Use SSL (legacy name for compatibility)
    /// </summary>
    public bool UseSSL
    {
        get => UseSsl;
        set => UseSsl = value;
    }

    /// <summary>
    /// Use passive mode (only valid for FTP)
    /// </summary>
    public bool UsePassiveMode { get; set; } = true;

    /// <summary>
    /// Additional connection options (in JSON format)
    /// </summary>
    public string? ConnectionOptions { get; set; }

    /// <summary>
    /// Server key verification (for SFTP)
    /// </summary>
    public bool VerifyServerFingerprint { get; set; } = false;

    /// <summary>
    /// Server fingerprint (for SFTP verification)
    /// </summary>
    public string? ServerFingerprint { get; set; }

    /// <summary>
    /// Private key file path (for SFTP)
    /// </summary>
    public string? PrivateKeyFile { get; set; }

    /// <summary>
    /// Private key passphrase (for SFTP)
    /// </summary>
    public string? PrivateKeyPassphrase { get; set; }

    /// <summary>
    /// Whether to calculate bucket sizes
    /// </summary>
    /// <remarks>
    /// This operation may require intensive CPU and network usage, and may have performance impact on systems with large numbers of files.
    /// </remarks>
    public bool CalculateBucketSize { get; set; } = false;
}
