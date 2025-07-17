namespace Qutora.API.Middleware;

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