namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// SFTP Provider options
/// </summary>
public class SftpProviderOptions
{
    /// <summary>
    /// Provider unique identifier
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

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
    /// Whether to calculate bucket/folder sizes
    /// </summary>
    /// <remarks>
    /// This operation may require intensive CPU and network usage, and may have performance impact on systems with large numbers of files.
    /// </remarks>
    public bool CalculateBucketSizes { get; set; } = false;
}
