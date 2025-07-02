namespace Qutora.Shared.Helpers;

/// <summary>
/// Helper class for file-related operations
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Formats file size in bytes to human readable format
    /// </summary>
    /// <param name="bytes">File size in bytes</param>
    /// <returns>Formatted file size string (e.g., "1.5 MB")</returns>
    public static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
} 