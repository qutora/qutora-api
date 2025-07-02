using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Bucket/klasör işlemlerini yöneten servis arayüzü
/// </summary>
public interface IStorageBucketService
{
    /// <summary>
    /// Belirli bir depolama sağlayıcısındaki bucket/klasörleri listeler
    /// </summary>
    /// <param name="providerId">Depolama sağlayıcısı ID'si</param>
    /// <returns>Bucket bilgileri listesi</returns>
    Task<IEnumerable<BucketInfoDto>> ListProviderBucketsAsync(string providerId);

    /// <summary>
    /// Kullanıcının belirli bir provider'daki erişim yetkisi olan bucket'ları getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="providerId">Depolama sağlayıcısı ID'si</param>
    /// <returns>Kullanıcının yetkili olduğu bucket bilgileri listesi</returns>
    Task<IEnumerable<BucketInfoDto>> GetUserAccessibleBucketsForProviderAsync(string userId, string providerId);

    /// <summary>
    /// Belirli bir provider'ın default bucket'ını getirir
    /// </summary>
    /// <param name="providerId">Depolama sağlayıcısı ID'si</param>
    /// <returns>Default bucket bilgisi veya null</returns>
    Task<BucketInfoDto?> GetDefaultBucketForProviderAsync(string providerId);

    /// <summary>
    /// Bir bucket/klasörün var olup olmadığını kontrol eder
    /// </summary>
    /// <param name="providerId">Depolama sağlayıcısı ID'si</param>
    /// <param name="bucketPath">Bucket/klasör path'i</param>
    /// <returns>Bucket/klasör varsa true, yoksa false</returns>
    Task<bool> BucketExistsAsync(string providerId, string bucketPath);

    /// <summary>
    /// Belirli bir bucket ID'si için bucket bilgilerini alır
    /// </summary>
    /// <param name="bucketId">Bucket ID'si</param>
    /// <returns>Bucket entity'si</returns>
    Task<StorageBucket> GetBucketByIdAsync(Guid bucketId);

    /// <summary>
    /// Bir bucket/klasörü siler
    /// </summary>
    /// <param name="providerId">Depolama sağlayıcısı ID'si</param>
    /// <param name="bucketPath">Bucket/klasör path'i</param>
    /// <param name="force">İçerik dolu olsa bile silme işlemi yapılsın mı</param>
    /// <returns>İşlem başarılıysa true, değilse false</returns>
    Task<bool> RemoveBucketAsync(string providerId, string bucketPath, bool force = false);

    /// <summary>
    /// Bucket izinlerini getirir
    /// </summary>
    /// <param name="bucketId">Bucket ID'si</param>
    /// <returns>Bucket izinleri listesi</returns>
    Task<IEnumerable<BucketPermissionDto>> GetBucketPermissionsAsync(Guid bucketId);

    /// <summary>
    /// Kullanıcının bucket izinlerini sayfalanmış olarak getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
    /// <returns>Sayfalanmış bucket izinleri sonuç nesnesi</returns>
    Task<PagedDto<BucketPermissionDto>> GetUserBucketPermissionsPaginatedAsync(string userId, int page, int pageSize);

    /// <summary>
    /// Gets all bucket permissions including both user and role permissions (paginated) - Admin only
    /// </summary>
    Task<PagedDto<BucketPermissionDto>> GetAllBucketPermissionsPaginatedAsync(int page, int pageSize);

    /// <summary>
    /// Sayfalandırılmış bucket listesini getirir
    /// </summary>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
    /// <returns>Sayfalandırılmış bucket listesi</returns>
    Task<IEnumerable<StorageBucket>> GetPaginatedBucketsAsync(int page, int pageSize);

    /// <summary>
    /// Kullanıcının erişim yetkisi olan bucketları sayfalandırılmış şekilde getirir
    /// </summary>
    /// <param name="userId">Kullanıcı ID'si</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa başına kayıt sayısı</param>
    /// <returns>Sayfalandırılmış bucket listesi</returns>
    Task<PagedDto<BucketInfoDto>> GetUserAccessiblePaginatedBucketsAsync(string userId, int page, int pageSize);

    /// <summary>
    /// Provider ID ve bucket path'ine göre bucket ID'sini alır
    /// </summary>
    /// <param name="providerId">Provider ID'si</param>
    /// <param name="bucketPath">Bucket path'i</param>
    /// <returns>Bucket ID'si veya null</returns>
    Task<Guid?> GetBucketIdByProviderAndPathAsync(string providerId, string bucketPath);

    /// <summary>
    /// Yeni bir bucket oluşturur (detaylı)
    /// </summary>
    /// <param name="dto">Bucket oluşturma DTO'su</param>
    /// <param name="userId">Oluşturan kullanıcı ID'si</param>
    /// <returns>Oluşturulan bucket entity'si</returns>
    Task<StorageBucket> CreateBucketAsync(BucketCreateDto dto, string userId);

    /// <summary>
    /// Bucket ID'sine göre bucket path'ini getirir
    /// </summary>
    /// <param name="bucketId">Bucket ID'si</param>
    /// <returns>Bucket path'i veya null</returns>
    Task<string?> GetBucketPathByIdAsync(Guid bucketId);

    /// <summary>
    /// Bucket path'ine göre bucket ID'sini getirir
    /// </summary>
    /// <param name="bucketPath">Bucket path'i</param>
    /// <returns>Bucket ID'si veya null</returns>
    Task<Guid?> GetBucketIdByPathAsync(string bucketPath);

    /// <summary>
    /// Bucket'ta doküman olup olmadığını kontrol eder
    /// </summary>
    /// <param name="bucketId">Bucket ID'si</param>
    /// <returns>Bucket'ta doküman varsa true, yoksa false</returns>
    Task<bool> HasDocumentsAsync(Guid bucketId);
}