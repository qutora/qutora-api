namespace Qutora.Infrastructure.Interfaces;

/// <summary>
/// İptal edilmiş token'ları yönetmek için servis arayüzü
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Bir token'ı blacklist'e ekler
    /// </summary>
    /// <param name="jti">Token'ın benzersiz tanımlayıcısı (JWT ID)</param>
    /// <param name="expiryTime">Token'ın geçerlilik süresi sonu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>İşlem başarısı</returns>
    Task<bool> AddToBlacklistAsync(string jti, DateTime expiryTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir token'ın blacklist'te olup olmadığını kontrol eder
    /// </summary>
    /// <param name="jti">Token'ın benzersiz tanımlayıcısı (JWT ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token blacklist'te ise true, değilse false</returns>
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Süresi dolmuş blacklist kayıtlarını temizler
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}