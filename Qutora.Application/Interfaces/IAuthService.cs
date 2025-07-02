using Qutora.Shared.DTOs.Authentication;
using Qutora.Shared.DTOs.Common;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Kimlik doğrulama işlemleri için servis arayüzü
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Kullanıcı giriş işlemi
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// İlk sistem kurulumu ve admin kullanıcı oluşturma
    /// </summary>
    Task<MessageResponseDto> InitialSetupAsync(InitialSetupRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Token yenileme işlemi
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı çıkış işlemi
    /// </summary>
    Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı bilgilerini getirme
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sistemin ilk kurulumunun tamamlanıp tamamlanmadığını kontrol eder
    /// </summary>
    Task<bool> IsSystemInitializedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının rollerini getirir
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcının yetkilerini/izinlerini getirir
    /// </summary>
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı profil bilgilerini getirir
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı profil bilgilerini günceller
    /// </summary>
    Task<MessageResponseDto> UpdateUserProfileAsync(string userId, UpdateUserProfileDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı şifresini değiştirir
    /// </summary>
    Task<MessageResponseDto> ChangePasswordAsync(string userId, ChangePasswordDto request, CancellationToken cancellationToken = default);
}