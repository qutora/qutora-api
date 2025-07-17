using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.DTOs.Authentication;
using Qutora.Shared.DTOs.Common;

namespace Qutora.Application.Identity;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    SignInManager<ApplicationUser> signInManager,
    IOptions<JwtSettings> jwtSettings,
    IMapper mapper,
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    ITokenBlacklistService tokenBlacklist,
    IHttpContextAccessor httpContextAccessor)
    : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
    private readonly JwtSettings _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ITokenBlacklistService _tokenBlacklist = tokenBlacklist ?? throw new ArgumentNullException(nameof(tokenBlacklist));
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = new AuthResponse();

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            response.Success = false;
            response.Message = "Invalid username or password";
            return response;
        }

        if (!user.IsActive)
        {
            response.Success = false;
            response.Message = "Your account is not active";
            return response;
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, request.Password, false, false);

        if (!result.Succeeded)
        {
            response.Success = false;
            response.Message = "Invalid username or password";
            return response;
        }

        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var userRoles = await _userManager.GetRolesAsync(user);

        var userPermissions = await GetUserPermissionsAsync(user.Id, cancellationToken);

        var jwtId = Guid.NewGuid().ToString();
        var tokenExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);

        var token = await GenerateJwtTokenAsync(user, userRoles, jwtId, cancellationToken);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);

        await RemoveOldRefreshTokensAsync(user, cancellationToken);

        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        user.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            JwtId = jwtId,
            ExpiryDate = refreshTokenExpiry,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        });

        await _userManager.UpdateAsync(user);

        response.Success = true;
        response.Token = token;
        response.RefreshToken = refreshToken;
        response.ExpiresAt = new DateTimeOffset(tokenExpires).ToUnixTimeSeconds();
        response.User = new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = userRoles.ToList(),
            Permissions = userPermissions.ToList()
        };

        return response;
    }

    public async Task<MessageResponseDto> InitialSetupAsync(InitialSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await IsSystemInitializedAsync(cancellationToken))
            return MessageResponseDto.ErrorResponse("System is already initialized.");

        try
        {
            return await ExecuteTransactionalAsync(async () =>
            {
                await EnsureRolesExistAsync(cancellationToken);

                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                    return MessageResponseDto.ErrorResponse("This email address is already in use");

                var adminUser = new ApplicationUser
                {
                    Email = request.Email,
                    UserName = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(adminUser, request.Password);

                if (!result.Succeeded)
                    return MessageResponseDto.ErrorResponse(string.Join(", ",
                        result.Errors.Select(e => e.Description)));

                await _userManager.AddToRoleAsync(adminUser, "Admin");

                var systemSettings = await _unitOfWork.SystemSettings.FirstOrDefaultAsync(cancellationToken);
                if (systemSettings != null)
                {
                    systemSettings.IsInitialized = true;
                    systemSettings.InitializedAt = DateTime.UtcNow;
                    systemSettings.InitializedByUserId = adminUser.Id;
                    systemSettings.ApplicationName = request.OrganizationName;
                    systemSettings.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.SystemSettings.Update(systemSettings);
                }
                else
                {
                    await _unitOfWork.SystemSettings.AddAsync(new SystemSettings
                    {
                        IsInitialized = true,
                        InitializedAt = DateTime.UtcNow,
                        InitializedByUserId = adminUser.Id,
                        ApplicationName = request.OrganizationName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }

                // Create default storage provider and bucket from configuration
                await CreateDefaultStorageProviderAndBucketAsync(adminUser.Id, cancellationToken);



                return MessageResponseDto.SuccessResponse("System setup completed successfully.");
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            return MessageResponseDto.ErrorResponse($"An error occurred during system setup: {ex.Message}");
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new AuthResponse();

        return await ExecuteTransactionalAsync(async () =>
        {
            // Validate refresh token directly from database - not JWT!
            var refreshToken = await _unitOfWork.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (refreshToken == null)
            {
                response.Success = false;
                response.Message = "Invalid refresh token";
                return response;
            }

            if (refreshToken.ExpiryDate < DateTime.UtcNow)
            {
                response.Success = false;
                response.Message = "Refresh token has expired";
                return response;
            }

            if (refreshToken.IsUsed)
            {
                response.Success = false;
                response.Message = "Refresh token has already been used";
                await RevokeAllUserTokensAsync(refreshToken.UserId, "Refresh token reuse detected", cancellationToken);
                return response;
            }

            if (refreshToken.IsRevoked)
            {
                response.Success = false;
                response.Message = "Refresh token has been revoked";
                await RevokeAllUserTokensAsync(refreshToken.UserId, "Revoked refresh token usage", cancellationToken);
                return response;
            }

            var user = await _userManager.FindByIdAsync(refreshToken.UserId);
            if (user == null || !user.IsActive)
            {
                response.Success = false;
                response.Message = "Invalid user";
                return response;
            }

            // If access token is also sent (optional) - we can validate it
            if (!string.IsNullOrEmpty(request.AccessToken))
            {
                try
                {
                    var validationParameters = GetValidationParameters();
                    validationParameters.ValidateLifetime = false; // May be expired

                    var tokenHandler = new JwtSecurityTokenHandler();
                    var principal = tokenHandler.ValidateToken(request.AccessToken, validationParameters, out var securityToken);
                    
                    var jwtId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                    
                    // Check if JWT ID matches with refresh token
                    if (jwtId != refreshToken.JwtId)
                    {
                        response.Success = false;
                        response.Message = "Token mismatch";
                        return response;
                    }

                    // Blacklist check
                    if (_jwtSettings.TokenBlacklistEnabled && !string.IsNullOrEmpty(jwtId))
                    {
                        if (await _tokenBlacklist.IsBlacklistedAsync(jwtId, cancellationToken))
                        {
                            response.Success = false;
                            response.Message = "This token has been invalidated";
                            return response;
                        }

                        // Add old token to blacklist
                        var expiry = principal.FindFirst("exp") != null
                            ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(principal.FindFirst("exp")!.Value)).UtcDateTime
                            : DateTime.UtcNow.AddDays(1);

                        await _tokenBlacklist.AddToBlacklistAsync(jwtId, expiry, cancellationToken);
                    }
                }
                catch (Exception)
                {
                    // Access token validation failed, but we can still continue with refresh
                    // since refresh token is valid
                }
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userPermissions = await GetUserPermissionsAsync(user.Id, cancellationToken);

            // Create new tokens
            var newJwtId = Guid.NewGuid().ToString();
            var tokenExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
            var token = await GenerateJwtTokenAsync(user, userRoles, newJwtId, cancellationToken);

            var newRefreshToken = GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays);
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            // Mark old refresh token as used
            refreshToken.IsUsed = true;
            refreshToken.ReplacedByToken = newRefreshToken;
            _unitOfWork.RefreshTokens.Update(refreshToken);

            // Create new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                JwtId = newJwtId,
                ExpiryDate = refreshTokenExpiry,
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };

            await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);

            response.Success = true;
            response.Token = token;
            response.RefreshToken = newRefreshToken;
            response.ExpiresAt = new DateTimeOffset(tokenExpires).ToUnixTimeSeconds();
            response.User = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = userRoles.ToList(),
                Permissions = userPermissions.ToList()
            };

            return response;
        }, cancellationToken);
    }

    public async Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        var refreshTokens = await _unitOfWork.RefreshTokens
            .FindAsync(rt => rt.UserId == userId && !rt.IsRevoked && DateTime.UtcNow < rt.ExpiryDate, cancellationToken);

        if (!refreshTokens.Any()) return false;

        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;

            if (_jwtSettings.TokenBlacklistEnabled && !string.IsNullOrEmpty(token.JwtId))
                await _tokenBlacklist.AddToBlacklistAsync(token.JwtId, token.ExpiryDate, cancellationToken);
        }

        _unitOfWork.RefreshTokens.UpdateRange(refreshTokens);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _signInManager.SignOutAsync();
        return true;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin,
            Roles = roles.ToList()
        };
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles, string? jwtId = null,
        CancellationToken cancellationToken = default)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, jwtId ?? Guid.NewGuid().ToString()),
            new("FirstName", user.FirstName),
            new("LastName", user.LastName)
        };

        var allPermissions = new HashSet<string>();

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));

            var roleObj = await _roleManager.FindByNameAsync(role);
            if (roleObj != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(roleObj);
                
                foreach (var roleClaim in roleClaims)
                {
                    if (roleClaim.Type == "permissions")
                    {
                        allPermissions.Add(roleClaim.Value);
                    }
                    else
                    {
                        claims.Add(roleClaim);
                    }
                }
            }
        }

        foreach (var permission in allPermissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        var token = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = _jwtSettings.ValidateIssuer,
            ValidateAudience = _jwtSettings.ValidateAudience,
            ValidateLifetime = _jwtSettings.ValidateLifetime,
            ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(_jwtSettings.AccessTokenClockSkew)
        };
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task RemoveOldRefreshTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var cutoff = DateTime.UtcNow.AddDays(-_jwtSettings.RefreshTokenExpiryDays);

            var oldTokens = await _unitOfWork.RefreshTokens
                .FindAsync(rt => rt.UserId == user.Id &&
                             (rt.IsUsed || rt.IsRevoked || rt.ExpiryDate < cutoff), cancellationToken);

            if (oldTokens.Any())
            {
                _unitOfWork.RefreshTokens.RemoveRange(oldTokens);
            }
        }, cancellationToken);
    }

    private async Task RevokeAllUserTokensAsync(string userId, string reason,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var refreshTokens = await _unitOfWork.RefreshTokens
                .FindAsync(rt => rt.UserId == userId && !rt.IsRevoked, cancellationToken);

            if (!refreshTokens.Any())
                return;

            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;

                if (_jwtSettings.TokenBlacklistEnabled && !string.IsNullOrEmpty(token.JwtId))
                    await _tokenBlacklist.AddToBlacklistAsync(token.JwtId, token.ExpiryDate, cancellationToken);
            }

            _unitOfWork.RefreshTokens.UpdateRange(refreshTokens);
        }, cancellationToken);
    }

    public async Task<bool> IsSystemInitializedAsync(CancellationToken cancellationToken = default)
    {
        var systemSettings = await _unitOfWork.SystemSettings.FirstOrDefaultAsync(cancellationToken);
        return systemSettings?.IsInitialized ?? false;
    }

    /// <summary>
    /// Ensures basic roles are created
    /// </summary>
    private async Task EnsureRolesExistAsync(CancellationToken cancellationToken = default)
    {
        // Admin Role - Full access
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            var adminRole = new ApplicationRole
            {
                Name = "Admin",
                Description = "System administrator with full privileges"
            };
            await _roleManager.CreateAsync(adminRole);

            var policiesSection = _configuration.GetSection("Authorization:Policies");
            var policies = policiesSection.GetChildren();
            var allPermissions = new HashSet<string>();

            foreach (var policy in policies)
            {
                var name = policy.GetValue<string>("Name");
                var permissions = policy.GetSection("RequiredPermissions").Get<List<string>>();

                if (permissions != null)
                    foreach (var permission in permissions)
                    {
                        allPermissions.Add(permission);
                    }
            }

            var roleAdmin = await _roleManager.FindByNameAsync("Admin");
            if (roleAdmin != null)
            {
                foreach (var permission in allPermissions)
                {
                    await _roleManager.AddClaimAsync(
                        roleAdmin,
                        new Claim("permissions", permission)
                    );
                }
            }
        }

        // Manager Role - Management level access
        if (!await _roleManager.RoleExistsAsync("Manager"))
        {
            var managerRole = new ApplicationRole
            {
                Name = "Manager",
                Description = "Department manager with management privileges"
            };
            await _roleManager.CreateAsync(managerRole);

            var policiesSection = _configuration.GetSection("Authorization:Policies");
            var policies = policiesSection.GetChildren();
            var managerPermissions = new HashSet<string>();

            foreach (var policy in policies)
            {
                var name = policy.GetValue<string>("Name");

                // Manager can manage documents, users, storage, view reports but cannot manage system settings
                if (name != null && (name.Contains("Document") ||
                                     name.Contains("User") ||
                                     name.Contains("Category") ||
                                     name.Contains("Metadata") ||
                                     name.Contains("Approval") ||
                                     name.Contains("Share") ||
                                     name.Contains("Storage") ||
                                     name.Contains("Bucket") ||
                                     name.EndsWith(".Read") ||
                                     name == "ApiKey.Manage"))
                {
                    var permissions = policy.GetSection("RequiredPermissions").Get<List<string>>();

                    if (permissions != null)
                        foreach (var permission in permissions)
                        {
                            managerPermissions.Add(permission);
                        }
                }
            }

            var roleManager = await _roleManager.FindByNameAsync("Manager");
            if (roleManager != null)
            {
                foreach (var permission in managerPermissions)
                {
                    await _roleManager.AddClaimAsync(
                        roleManager,
                        new Claim("permissions", permission)
                    );
                }
            }
        }

        // Inspector Role - Inspection and audit access
        if (!await _roleManager.RoleExistsAsync("Inspector"))
        {
            var inspectorRole = new ApplicationRole
            {
                Name = "Inspector",
                Description = "Inspection officer with audit and review privileges"
            };
            await _roleManager.CreateAsync(inspectorRole);

            var policiesSection = _configuration.GetSection("Authorization:Policies");
            var policies = policiesSection.GetChildren();
            var inspectorPermissions = new HashSet<string>();

            foreach (var policy in policies)
            {
                var name = policy.GetValue<string>("Name");

                // Inspector can read all content for inspection purposes, but cannot modify
                if (name != null && (name.EndsWith(".Read") ||
                                     name.Contains("Document.Read") ||
                                     name.Contains("User.Read") ||
                                     name.Contains("Category.Read") ||
                                     name.Contains("Metadata.Read") ||
                                     name.Contains("Approval.Read") ||
                                     name.Contains("Share.Read") ||
                                     name.Contains("Bucket.Read")))
                {
                    var permissions = policy.GetSection("RequiredPermissions").Get<List<string>>();

                    if (permissions != null)
                        foreach (var permission in permissions)
                        {
                            inspectorPermissions.Add(permission);
                        }
                }
            }

            var roleInspector = await _roleManager.FindByNameAsync("Inspector");
            if (roleInspector != null)
            {
                foreach (var permission in inspectorPermissions)
                {
                    await _roleManager.AddClaimAsync(
                        roleInspector,
                        new Claim("permissions", permission)
                    );
                }
            }
        }

        // InternalAuditor Role - Internal audit access
        if (!await _roleManager.RoleExistsAsync("InternalAuditor"))
        {
            var internalAuditorRole = new ApplicationRole
            {
                Name = "InternalAuditor",
                Description = "Internal auditor with comprehensive review privileges"
            };
            await _roleManager.CreateAsync(internalAuditorRole);

            var policiesSection = _configuration.GetSection("Authorization:Policies");
            var policies = policiesSection.GetChildren();
            var auditorPermissions = new HashSet<string>();

            foreach (var policy in policies)
            {
                var name = policy.GetValue<string>("Name");

                // InternalAuditor has focused read access for audit purposes
                // Added full User management permissions for UI access
                if (name != null && (name.Contains("Document.Read") ||
                                     name.Contains("User.Read") ||
                                     name.Contains("User.Create") ||
                                     name.Contains("User.Update") ||
                                     name.Contains("User.Delete") ||
                                     name.Contains("Approval.Read") ||
                                     name.Contains("Share.Read") ||
                                     name.Contains("ApiKey.Read") ||
                                     name.Contains("StorageProvider.Read")))
                {
                    var permissions = policy.GetSection("RequiredPermissions").Get<List<string>>();

                    if (permissions != null)
                        foreach (var permission in permissions)
                        {
                            auditorPermissions.Add(permission);
                        }
                }
            }

            var roleAuditor = await _roleManager.FindByNameAsync("InternalAuditor");
            if (roleAuditor != null)
            {
                foreach (var permission in auditorPermissions)
                {
                    await _roleManager.AddClaimAsync(
                        roleAuditor,
                        new Claim("permissions", permission)
                    );
                }
            }
        }

        // User Role - Standard user access
        if (!await _roleManager.RoleExistsAsync("User"))
        {
            var userRole = new ApplicationRole
            {
                Name = "User",
                Description = "Standard user with basic privileges"
            };
            await _roleManager.CreateAsync(userRole);

            var policiesSection = _configuration.GetSection("Authorization:Policies");
            var policies = policiesSection.GetChildren();
            var userPermissions = new HashSet<string>();

            foreach (var policy in policies)
            {
                var name = policy.GetValue<string>("Name");

                // User can read all content, perform basic document operations, and manage their own API keys for SDK integration
                if (name != null && (name.EndsWith(".Read") ||
                                     name == "Document.Create" ||
                                     name == "Document.Update" ||
                                     name == "Document.Share" ||
                                     name == "Category.Create" ||
                                     name == "Metadata.Create" ||
                                     name == "Metadata.Update" ||
                                     name == "StorageProvider.Read" ||
                                     name == "ApiKey.Manage" ||
                                     name == "Bucket.Read" ||
                                     name == "Bucket.Write"))
                {
                    var permissions = policy.GetSection("RequiredPermissions").Get<List<string>>();

                    if (permissions != null)
                        foreach (var permission in permissions)
                        {
                            userPermissions.Add(permission);
                        }
                }
            }

            var roleStandard = await _roleManager.FindByNameAsync("User");
            if (roleStandard != null)
            {
                foreach (var permission in userPermissions)
                {
                    await _roleManager.AddClaimAsync(
                        roleStandard,
                        new Claim("permissions", permission)
                    );
                }
            }
        }
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return [];

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return [];

        var roles = await _userManager.GetRolesAsync(user);

        var permissions = new HashSet<string>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var roleClaims = await _roleManager.GetClaimsAsync(role);
                var rolePermissions = roleClaims
                    .Where(c => c.Type == "permissions")
                    .Select(c => c.Value);

                foreach (var permission in rolePermissions) permissions.Add(permission);
            }
        }

        return permissions;
    }

    /// <summary>
    /// Helper method to execute an action within a transaction
    /// </summary>
    private async Task<T> ExecuteTransactionalAsync<T>(Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteTransactionalAsync(action, cancellationToken);
    }

    /// <summary>
    /// Creates default storage provider and bucket from configuration during initial setup
    /// </summary>
    private async Task CreateDefaultStorageProviderAndBucketAsync(string createdByUserId, CancellationToken cancellationToken = default)
    {
        // Get storage configuration from environment/appsettings - REQUIRED
        var defaultRootPath = _configuration["Storage:DefaultProvider:RootPath"];
        var defaultBucketPath = _configuration["Storage:DefaultBucket:Path"];

        // Validate required configuration values
        if (string.IsNullOrWhiteSpace(defaultRootPath))
        {
            throw new InvalidOperationException("Storage:DefaultProvider:RootPath configuration is required but not provided. Please set this environment variable before initializing the system.");
        }

        if (string.IsNullOrWhiteSpace(defaultBucketPath))
        {
            throw new InvalidOperationException("Storage:DefaultBucket:Path configuration is required but not provided. Please set this environment variable before initializing the system.");
        }

        // Normalize path for current platform (Windows: \, Linux/macOS: /)
        var normalizedRootPath = defaultRootPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

        // Create default storage provider
        var defaultProviderId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        // Create config object and serialize to JSON safely
        var config = new { RootPath = normalizedRootPath, CreateDirectoryIfNotExists = true };
        var configJson = System.Text.Json.JsonSerializer.Serialize(config);
        
        var storageProvider = new StorageProvider
        {
            Id = defaultProviderId,
            Name = "Local Storage",
            ProviderType = "filesystem",
            ConfigJson = configJson,
            IsDefault = true,
            IsActive = true,
            Description = "Local file system storage",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        };

        await _unitOfWork.StorageProviders.AddAsync(storageProvider, cancellationToken);

        // Create default storage bucket
        var defaultBucketId = Guid.Parse("00000000-0000-0000-0001-000000000001");
        var storageBucket = new StorageBucket
        {
            Id = defaultBucketId,
            Path = defaultBucketPath,
            Description = "General purpose file storage bucket",
            ProviderId = defaultProviderId,
            IsDefault = true,
            IsActive = true,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        };

        await _unitOfWork.StorageBuckets.AddAsync(storageBucket, cancellationToken);

        // Create physical directories
        try
        {
            var fullRootPath = Path.GetFullPath(normalizedRootPath);
            if (!Directory.Exists(fullRootPath))
            {
                Directory.CreateDirectory(fullRootPath);
            }

            var bucketFullPath = Path.Combine(fullRootPath, defaultBucketPath);
            if (!Directory.Exists(bucketFullPath))
            {
                Directory.CreateDirectory(bucketFullPath);
            }

            System.Diagnostics.Debug.WriteLine($"✅ Created storage directories - Root: {fullRootPath}, Bucket: {bucketFullPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error creating storage directories: {ex.Message}");
            // Don't fail the setup process if directory creation fails
        }
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? "User";

        return new UserProfileDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Role = primaryRole,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLogin,
            IsActive = user.IsActive
        };
    }

    public async Task<MessageResponseDto> UpdateUserProfileAsync(string userId, UpdateUserProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return MessageResponseDto.ErrorResponse("User not found");

        // Check if email is being changed and if it's already in use
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != userId)
                return MessageResponseDto.ErrorResponse("Email address is already in use");
        }

        // Update user properties
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.UserName = request.Email; // Username is same as email
        user.NormalizedEmail = request.Email.ToUpper();
        user.NormalizedUserName = request.Email.ToUpper();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return MessageResponseDto.ErrorResponse($"Profile update failed: {errors}");
        }

        return MessageResponseDto.SuccessResponse("Profile updated successfully");
    }

    public async Task<MessageResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return MessageResponseDto.ErrorResponse("User not found");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return MessageResponseDto.ErrorResponse($"Password change failed: {errors}");
        }

        return MessageResponseDto.SuccessResponse("Password changed successfully");
    }
}
