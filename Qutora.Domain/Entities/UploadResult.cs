namespace Qutora.Domain.Entities;

/// <summary>
/// Represents the result of a file upload operation.
/// </summary>
public class UploadResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the upload was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the path where the file was stored in the storage provider.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file's unique identifier.
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider ID used for the upload.
    /// </summary>
    public int? ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the provider name used for the upload.
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the content type of the uploaded file.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file hash (if calculated during upload).
    /// </summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if the upload was not successful.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the upload was completed.
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}