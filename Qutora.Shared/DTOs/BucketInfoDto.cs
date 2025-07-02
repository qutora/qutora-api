namespace Qutora.Shared.DTOs;

public class BucketInfoDto
{
    /// <summary>
    /// Bucket ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Bucket path
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Bucket description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// User's permission level on this bucket
    /// </summary>
    public Enums.PermissionLevel Permission { get; set; }

    public DateTime CreationDate { get; set; }
    public long? Size { get; set; }
    public int? ObjectCount { get; set; }
    public string ProviderType { get; set; }
    public string ProviderName { get; set; }
    public string ProviderId { get; set; }
}