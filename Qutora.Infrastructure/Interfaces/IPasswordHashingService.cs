namespace Qutora.Infrastructure.Interfaces;

/// <summary>
/// Şifre hash'leme işlemleri için servis interface'i.
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Şifreyi hash'ler.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Şifreyi doğrular.
    /// </summary>
    bool VerifyPassword(string password, string hash);
}