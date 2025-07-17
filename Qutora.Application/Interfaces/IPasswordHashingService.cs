namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for password hashing operations.
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hashes the password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies the password.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}