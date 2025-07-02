using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Denetim kaydı (audit log) için servis arayüzü.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Audit log kaydı ekler
    /// </summary>
    Task LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// API isteği denetim kaydı ekler.
    /// </summary>
    /// <param name="auditLog">Denetim kaydı</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogApiRequestAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Genel aktivite kaydı ekler
    /// </summary>
    /// <param name="entityType">İşlem yapılan varlık tipi</param>
    /// <param name="entityId">İşlem yapılan varlık ID'si</param>
    /// <param name="action">İşlem tipi</param>
    /// <param name="details">İşlem detayları</param>
    /// <param name="additionalData">Ek bilgiler (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogActivityAsync(
        string entityType,
        string entityId,
        string action,
        string details,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Doküman versiyonu oluşturma denetim kaydı ekler.
    /// </summary>
    /// <param name="userId">İşlemi yapan kullanıcı kimliği</param>
    /// <param name="documentId">Doküman kimliği</param>
    /// <param name="versionId">Oluşturulan versiyon kimliği</param>
    /// <param name="versionNumber">Versiyon numarası</param>
    /// <param name="additionalData">Ek bilgiler (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentVersionCreatedAsync(
        string userId,
        Guid documentId,
        Guid versionId,
        int versionNumber,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Doküman versiyonu geri yükleme denetim kaydı ekler.
    /// </summary>
    /// <param name="userId">İşlemi yapan kullanıcı kimliği</param>
    /// <param name="documentId">Doküman kimliği</param>
    /// <param name="versionId">Geri yüklenen versiyon kimliği</param>
    /// <param name="versionNumber">Versiyon numarası</param>
    /// <param name="additionalData">Ek bilgiler (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentVersionRolledBackAsync(
        string userId,
        Guid documentId,
        Guid versionId,
        int versionNumber,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Doküman oluşturma denetim kaydı ekler.
    /// </summary>
    /// <param name="userId">İşlemi yapan kullanıcı kimliği</param>
    /// <param name="documentId">Doküman kimliği</param>
    /// <param name="documentName">Doküman adı</param>
    /// <param name="additionalData">Ek bilgiler (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentCreatedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Doküman güncelleme denetim kaydı ekler.
    /// </summary>
    /// <param name="userId">İşlemi yapan kullanıcı kimliği</param>
    /// <param name="documentId">Doküman kimliği</param>
    /// <param name="documentName">Doküman adı</param>
    /// <param name="changes">Yapılan değişiklikler</param>
    /// <param name="additionalData">Ek bilgiler (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentUpdatedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, object> changes,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Doküman silme denetim kaydı ekler.
    /// </summary>
    /// <param name="userId">İşlemi yapan kullanıcı kimliği</param>
    /// <param name="documentId">Doküman kimliği</param>
    /// <param name="documentName">Doküman adı</param>
    /// <param name="additionalData">Ek bilgiler (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogDocumentDeletedAsync(
        string userId,
        Guid documentId,
        string documentName,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcı işlemleri denetim kaydı ekler.
    /// </summary>
    /// <param name="userId">İşlemi yapan kullanıcı kimliği</param>
    /// <param name="targetUserId">Hedef kullanıcı kimliği</param>
    /// <param name="action">İşlem tipi (create, update, delete, etc.)</param>
    /// <param name="additionalData">Ek bilgiler (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogUserActionAsync(
        string userId,
        string targetUserId,
        string action,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sistem ayarları değişiklik denetim kaydı ekler.
    /// </summary>
    /// <param name="userId">İşlemi yapan kullanıcı kimliği</param>
    /// <param name="settingName">Ayarlar adı</param>
    /// <param name="oldValue">Eski değer</param>
    /// <param name="newValue">Yeni değer</param>
    /// <param name="additionalData">Ek bilgiler (opsiyonel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogSettingsChangedAsync(
        string userId,
        string settingName,
        string oldValue,
        string newValue,
        Dictionary<string, string>? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli bir kullanıcının audit loglarını getirir
    /// </summary>
    Task<IEnumerable<AuditLogDto>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API Key aktivitelerini getirir
    /// </summary>
    /// <param name="apiKeyId">API Key ID'si</param>
    /// <param name="startDate">Başlangıç tarihi (opsiyonel)</param>
    /// <param name="endDate">Bitiş tarihi (opsiyonel)</param>
    /// <param name="page">Sayfa numarası</param>
    /// <param name="pageSize">Sayfa boyutu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API Key aktivite logları ve toplam sayı</returns>
    Task<(IEnumerable<AuditLogDto> Activities, int TotalCount)> GetApiKeyActivitiesAsync(
        string apiKeyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}