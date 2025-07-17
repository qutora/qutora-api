using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;

namespace Qutora.Infrastructure.Services;

/// <summary>
/// Service implementation for password hashing operations using BCrypt
/// </summary>
public class PasswordHashingService(ILogger<PasswordHashingService> logger) : IPasswordHashingService
{
    private readonly ILogger<PasswordHashingService>
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Hashes password using BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        try
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hashing password");
            throw;
        }
    }

    /// <summary>
    /// Verifies password using BCrypt
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }
}
