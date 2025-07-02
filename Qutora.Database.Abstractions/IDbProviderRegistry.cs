namespace Qutora.Database.Abstractions;

/// <summary>
/// Veritabanı sağlayıcılarını yönetmek için registry
/// </summary>
public interface IDbProviderRegistry
{
    /// <summary>
    /// Bir veritabanı sağlayıcısını kaydeder
    /// </summary>
    /// <param name="provider">Sağlayıcı</param>
    void RegisterProvider(IDbProvider provider);

    /// <summary>
    /// İsim ile sağlayıcı alır
    /// </summary>
    /// <param name="providerName">Sağlayıcı adı</param>
    /// <returns>Sağlayıcı implementasyonu veya null</returns>
    IDbProvider? GetProvider(string providerName);

    /// <summary>
    /// Kayıtlı tüm sağlayıcı adlarını döndürür
    /// </summary>
    /// <returns>Sağlayıcı adları</returns>
    IEnumerable<string> GetAvailableProviders();

    /// <summary>
    /// DbContext sağlayıcı adını çıkarır
    /// </summary>
    /// <param name="providerName">EF Core Provider adı</param>
    /// <returns>Sağlayıcı adı</returns>
    string ExtractProviderName(string? providerName);
}