using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Qutora.Application.Identity;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.UnitOfWork;

namespace Qutora.API.Authentication;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyService apiKeyService,
    IUnitOfWork unitOfWork,
    IOptions<ApiKeyPermissionSettings> permissionSettings)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly ApiKeyPermissionSettings _permissionSettings = permissionSettings.Value;
    private const string API_KEY_HEADER = "X-QUTORA-Key";
    private const string API_SECRET_HEADER = "X-QUTORA-Secret";

    /// <summary>
    /// Authenticates the request using API key and secret from request headers
    /// </summary>
    /// <returns>Authentication result indicating success or failure</returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(API_KEY_HEADER, out var apiKeyHeaderValues) ||
            !Request.Headers.TryGetValue(API_SECRET_HEADER, out var apiSecretHeaderValues))
            return AuthenticateResult.NoResult();

        var key = apiKeyHeaderValues.ToString();
        var secret = apiSecretHeaderValues.ToString();

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret)) return AuthenticateResult.NoResult();

        try
        {
            if (await apiKeyService.ValidateApiKeyAsync(key, secret))
            {
                var apiKeyEntity = await unitOfWork.ApiKeys.GetByKeyAsync(key);

                var permissionClaims = new List<Claim>();

                var permissions = _permissionSettings.GetPermissionsForType(apiKeyEntity.Permissions);

                foreach (var permission in permissions) permissionClaims.Add(new Claim("permissions", permission));

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, apiKeyEntity.UserId),
                    new("ApiKeyId", apiKeyEntity.Id.ToString()),
                    new("ApiKeyPermission", apiKeyEntity.Permissions.ToString())
                };

                claims.AddRange(permissionClaims);

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                _ = unitOfWork.ApiKeys.UpdateLastUsedAsync(apiKeyEntity.Id);

                return AuthenticateResult.Success(ticket);
            }
            else
            {
                return AuthenticateResult.Fail("Invalid API key or secret");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating API key and secret");
            return AuthenticateResult.Fail("Error validating API credentials");
        }
    }
}
