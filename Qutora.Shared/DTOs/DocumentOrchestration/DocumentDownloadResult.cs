namespace Qutora.Shared.DTOs.DocumentOrchestration;

/// <summary>
/// Document download result
/// </summary>
public class DocumentDownloadResult
{
    public bool IsSuccess { get; set; }
    public Stream? FileStream { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static DocumentDownloadResult Success(Stream fileStream, string fileName, string contentType) => 
        new() { IsSuccess = true, FileStream = fileStream, FileName = fileName, ContentType = contentType };
    
    public static DocumentDownloadResult Failure(string errorMessage) => 
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}