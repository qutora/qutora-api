using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Doküman metadata işlemleri için servis arayüzü
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Bir dokümanın metadata bilgilerini getirir
    /// </summary>
    Task<MetadataDto?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir dokümanın metadata bilgilerini getirir
    /// </summary>
    Task<MetadataDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yeni bir metadata oluşturur
    /// </summary>
    Task<MetadataDto> CreateAsync(Guid documentId, CreateUpdateMetadataDto createMetadataDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Var olan bir metadata'yı günceller
    /// </summary>
    Task<MetadataDto> UpdateAsync(Guid documentId, CreateUpdateMetadataDto updateMetadataDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir metadata'yı siler
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirli etiketlere sahip dokümanların metadata bilgilerini sayfalı olarak getirir
    /// </summary>
    Task<PagedDto<MetadataDto>> GetByTagsAsync(string[] tags, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Metadata değerlerine göre dokümanları sayfalı olarak arar
    /// </summary>
    Task<PagedDto<MetadataDto>> SearchAsync(Dictionary<string, object> searchCriteria, int page = 1,
        int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Metadata validasyonu yapar
    /// </summary>
    Task<Dictionary<string, string>> ValidateMetadataAsync(string schemaName,
        Dictionary<string, object> metadata, CancellationToken cancellationToken = default);
}