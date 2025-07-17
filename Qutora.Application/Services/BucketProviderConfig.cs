namespace Qutora.Application.Services;

/// <summary>
/// Bucket provider configuration for each provider type
/// </summary>
internal class BucketProviderConfig
{
    public int MinBucketNameLength { get; set; }
    public int MaxBucketNameLength { get; set; }
    public required string AllowedCharactersPattern { get; set; }
    public bool AllowNestedBuckets { get; set; }
    public bool RequiresPermissionCheck { get; set; }
    public bool AllowForceDelete { get; set; }
    public required Func<string, (bool isValid, string message)> ValidateBucketName { get; set; }
}