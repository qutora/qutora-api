namespace Qutora.Infrastructure.Storage.Registry;

/// <summary>
/// Attribute that specifies the storage provider type.
/// Classes marked with this attribute are automatically registered to StorageProviderTypeRegistry.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProviderTypeAttribute : Attribute
{
    /// <summary>
    /// Storage provider type (e.g. "filesystem", "s3", "minio")
    /// </summary>
    public string ProviderType { get; }

    /// <summary>
    /// Creates an attribute that specifies the storage provider type.
    /// </summary>
    /// <param name="providerType">Storage provider type</param>
    public ProviderTypeAttribute(string providerType)
    {
        ProviderType = providerType ?? throw new ArgumentNullException(nameof(providerType));
    }
}
