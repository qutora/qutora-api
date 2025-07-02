namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Storage selection result container
/// </summary>
public class StorageSelectionResult
{
    public bool IsSuccess { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? BucketId { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static StorageSelectionResult Success(Guid providerId, Guid bucketId) => 
        new() { IsSuccess = true, ProviderId = providerId, BucketId = bucketId };
    
    public static StorageSelectionResult Failure(string errorMessage) => 
        new() { IsSuccess = false, ErrorMessage = errorMessage };
} 