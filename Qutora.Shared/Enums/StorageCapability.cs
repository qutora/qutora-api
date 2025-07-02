namespace Qutora.Shared.Enums;

/// <summary>
/// Capabilities/features supported by storage providers
/// </summary>
public enum StorageCapability
{
    /// <summary>
    /// Ability to list buckets/folders
    /// </summary>
    BucketListing,

    /// <summary>
    /// Ability to check if bucket/folder exists
    /// </summary>
    BucketExistence,

    /// <summary>
    /// Ability to create buckets/folders
    /// </summary>
    BucketCreation,

    /// <summary>
    /// Ability to delete buckets/folders
    /// </summary>
    BucketDeletion,

    /// <summary>
    /// Support for nested buckets/folders
    /// </summary>
    NestedBuckets,

    /// <summary>
    /// Ability to force delete operation (delete non-empty buckets)
    /// </summary>
    ForceDelete,

    /// <summary>
    /// Object versioning support
    /// </summary>
    ObjectVersioning,

    /// <summary>
    /// Object metadata support
    /// </summary>
    ObjectMetadata,

    /// <summary>
    /// Object ACL (access control list) support 
    /// </summary>
    ObjectAcl,

    /// <summary>
    /// Bucket ACL (access control list) support
    /// </summary>
    BucketAcl,

    /// <summary>
    /// Bucket lifecycle management support
    /// </summary>
    BucketLifecycle
}