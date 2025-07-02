using System.Security.Claims;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Authentication;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Kullanıcı yönetimi için servis arayüzü
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Tüm kullanıcıları listeler
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Sayfalanmış kullanıcı listesi getirir
    /// </summary>
    Task<PagedDto<UserDto>> GetPagedUsersAsync(int page = 1, int pageSize = 10, string? searchTerm = null);

    /// <summary>
    /// ID'ye göre kullanıcı bilgilerini getirir
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string userId);

    /// <summary>
    /// E-posta adresine göre kullanıcı bilgilerini getirir
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Yeni bir kullanıcı oluşturur (sadece admin yetkisiyle)
    /// </summary>
    Task<UserDto> CreateUserAsync(CreateUserRequest request);

    /// <summary>
    /// Kullanıcı bilgilerini günceller
    /// </summary>
    Task<UserDto> UpdateUserAsync(string userId, UserDto user);

    /// <summary>
    /// Kullanıcı şifresini değiştirir
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    /// <summary>
    /// Kullanıcı durumunu günceller (aktif/pasif)
    /// </summary>
    Task<bool> UpdateUserStatusAsync(string userId, bool isActive);

    /// <summary>
    /// Kullanıcı durumunu günceller (aktif/pasif) - güvenlik kontrolleri ile
    /// </summary>
    Task<bool> UpdateUserStatusAsync(string userId, bool isActive, string? currentUserId);

    /// <summary>
    /// Kullanıcıyı siler
    /// </summary>
    Task<bool> DeleteUserAsync(string userId);

    /// <summary>
    /// Kullanıcıyı siler - güvenlik kontrolleri ile
    /// </summary>
    Task<bool> DeleteUserAsync(string userId, string? currentUserId);

    /// <summary>
    /// Kullanıcıya rol atar
    /// </summary>
    Task<bool> AssignRoleToUserAsync(string userId, string roleName);

    /// <summary>
    /// Kullanıcıdan rol kaldırır
    /// </summary>
    Task<bool> RemoveRoleFromUserAsync(string userId, string roleName);

    /// <summary>
    /// Kullanıcının tüm rollerini getirir
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Sistemdeki tüm rolleri listeler
    /// </summary>
    Task<IEnumerable<string>> GetAllRolesAsync();

    /// <summary>
    /// Sistemdeki tüm rolleri ID ve Name ile birlikte listeler
    /// </summary>
    Task<IEnumerable<(string Id, string Name)>> GetAllRolesWithIdsAsync();

    /// <summary>
    /// Bir rolün sahip olduğu tüm claim'leri getirir
    /// </summary>
    Task<IEnumerable<Claim>> GetRoleClaimsAsync(string roleName);

    /// <summary>
    /// Bir role claim ekler
    /// </summary>
    Task<bool> AddClaimToRoleAsync(string roleName, string claimType, string claimValue);

    /// <summary>
    /// Bir rolden claim kaldırır
    /// </summary>
    Task<bool> RemoveClaimFromRoleAsync(string roleName, string claimType, string claimValue);

    /// <summary>
    /// Sistemdeki tüm izinleri kategorilendirilmiş şekilde döndürür
    /// </summary>
    Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();

    /// <summary>
    /// Bir rolün sahip olduğu izinleri döndürür
    /// </summary>
    Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName);

    /// <summary>
    /// Bir rol için izinleri günceller
    /// </summary>
    Task<bool> UpdateRolePermissionsAsync(string roleName, IEnumerable<string> permissions);

    /// <summary>
    /// Rol oluşturur
    /// </summary>
    Task<bool> CreateRoleAsync(string roleName, string roleDescription = "");

    /// <summary>
    /// Rol siler
    /// </summary>
    Task<bool> DeleteRoleAsync(string roleName);

    /// <summary>
    /// Belirtilen roldeki kullanıcıları getirir
    /// </summary>
    Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName);
}