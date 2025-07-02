using Qutora.API.Middleware;

namespace Qutora.API.Extensions;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds API key authentication middleware to the application
    /// </summary>
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }

    /// <summary>
    /// Adds JWT token validation middleware to the application
    /// </summary>
    public static IApplicationBuilder UseJwtTokenValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtTokenValidationMiddleware>();
    }
}
