namespace Qutora.Shared.DTOs;

/// <summary>
/// DTO for storage provider connection testing
/// </summary>
public class StorageProviderTestDto
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Provider type (FileSystem, S3, FTP, SFTP, etc.)
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// Provider configuration (in JSON format)
    /// </summary>
    public string ConfigJson { get; set; } = string.Empty;

    /// <summary>
    /// Was the test successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Test result message
    /// </summary>
    public string? Message { get; set; }
}