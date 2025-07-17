namespace Qutora.API.Middleware;

/// <summary>
/// Configuration settings for PublicViewer authentication
/// </summary>
public class PublicViewerOptions
{
    public const string SectionName = "PublicViewer";

    /// <summary>
    /// API key for public viewer authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// List of allowed IP addresses for public viewer
    /// </summary>
    public string[] AllowedIPs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether to allow localhost access (for development)
    /// </summary>
    public bool AllowLocalhost { get; set; } = false;
}