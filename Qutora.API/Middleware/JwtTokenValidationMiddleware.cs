using System.IdentityModel.Tokens.Jwt;
using Qutora.Application.Interfaces;

namespace Qutora.API.Middleware;

/// <summary>
/// JWT token validation middleware
/// </summary>
public class JwtTokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtTokenValidationMiddleware> _logger;
    private readonly ITokenBlacklistService _tokenBlacklist;

    public JwtTokenValidationMiddleware(
        RequestDelegate next,
        ILogger<JwtTokenValidationMiddleware> logger,
        ITokenBlacklistService tokenBlacklist)
    {
        _next = next;
        _logger = logger;
        _tokenBlacklist = tokenBlacklist;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);

                if (jtiClaim != null)
                {
                    var isBlacklisted = await _tokenBlacklist.IsBlacklistedAsync(jtiClaim.Value);

                    if (isBlacklisted)
                    {
                        _logger.LogWarning("Blacklisted token usage detected. JTI: {Jti}",
                            jtiClaim.Value);
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            "{\"error\":\"Token has been invalidated. Please log in again.\"}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred during JWT token reading/validation");
            }
        }
        await _next(context);
    }
}
