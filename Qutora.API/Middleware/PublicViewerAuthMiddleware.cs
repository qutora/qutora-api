using Microsoft.Extensions.Options;

namespace Qutora.API.Middleware;

/// <summary>
/// Middleware to secure public viewer API endpoints with IP and API key validation
/// </summary>
public class PublicViewerAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PublicViewerAuthMiddleware> _logger;
    private readonly PublicViewerOptions _options;

    public PublicViewerAuthMiddleware(
        RequestDelegate next, 
        ILogger<PublicViewerAuthMiddleware> logger,
        IOptions<PublicViewerOptions> settings)
    {
        _next = next;
        _logger = logger;
        _options = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only check shared-documents endpoints
        if (context.Request.Path.StartsWithSegments("/api/shared-documents"))
        {
            if (!IsValidPublicViewerRequest(context))
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                
                _logger.LogWarning(
                    "Unauthorized access attempt to shared documents API. IP: {IP}, User-Agent: {UserAgent}, Path: {Path}",
                    clientIp, userAgent, context.Request.Path);

                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(@"{""error"": ""Access denied. This endpoint is only accessible from the Public Document Viewer.""}");
                return;
            }

            _logger.LogDebug("Valid public viewer request from IP: {IP}", context.Connection.RemoteIpAddress);
        }

        await _next(context);
    }

    private bool IsValidPublicViewerRequest(HttpContext context)
    {
        return ValidateIP(context) && ValidateApiKey(context);
    }

    private bool ValidateIP(HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString();
        
        if (string.IsNullOrEmpty(clientIp))
        {
            _logger.LogWarning("Unable to determine client IP address");
            return false;
        }

        // Allow localhost for development
        if (_options.AllowLocalhost && (clientIp == "127.0.0.1" || clientIp == "::1"))
        {
            return true;
        }

        // Check allowed IPs
        if (_options.AllowedIPs?.Contains(clientIp) == true)
        {
            return true;
        }

        _logger.LogWarning("IP address {IP} is not in allowed list", clientIp);
        return false;
    }

    private bool ValidateApiKey(HttpContext context)
    {
        var providedKey = context.Request.Headers["X-Qutora-PublicViewer-Key"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(providedKey))
        {
            _logger.LogWarning("Missing X-Qutora-PublicViewer-Key header");
            return false;
        }

        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogError("PublicViewer ApiKey is not configured");
            return false;
        }

        if (providedKey != _options.ApiKey)
        {
            _logger.LogWarning("Invalid API key provided: {ProvidedKey}", providedKey);
            return false;
        }

        return true;
    }
}

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

/// <summary>
/// Extension methods for registering PublicViewer middleware
/// </summary>
public static class PublicViewerAuthMiddlewareExtensions
{
    public static IServiceCollection AddPublicViewerAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PublicViewerOptions>(configuration.GetSection(PublicViewerOptions.SectionName));
        return services;
    }

    public static IApplicationBuilder UsePublicViewerAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PublicViewerAuthMiddleware>();
    }
} 