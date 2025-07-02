using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for document sharing operations.
/// </summary>
public interface IDocumentShareService
{
    /// <summary>
    /// Gets a document share by ID.
    /// </summary>
    Task<DocumentShareDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document share by share code.
    /// </summary>
    Task<DocumentShareDto?> GetByShareCodeAsync(string shareCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets shares belonging to a document.
    /// </summary>
    Task<IEnumerable<DocumentShareDto>> GetByDocumentIdAsync(Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current user's shares.
    /// </summary>
    Task<IEnumerable<DocumentShareDto>> GetMySharesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's paginated shares.
    /// </summary>
    Task<PagedDto<DocumentShareDto>> GetUserSharesPagedAsync(string userId, int page = 1, int pageSize = 10, string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new document share.
    /// </summary>
    Task<DocumentShareDto> CreateShareAsync(DocumentShareCreateDto shareDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document share.
    /// </summary>
    Task<DocumentShareDto?> UpdateShareAsync(Guid id, DocumentShareCreateDto updateDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a document share (hard delete).
    /// </summary>
    Task<bool> DeleteShareAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles document share status (active/inactive).
    /// </summary>
    Task<bool> ToggleShareStatusAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets share access information (for management).
    /// </summary>
    Task<ShareAccessInfoDto> GetShareAccessInfoAsync(string shareCode, string ipAddress, string userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a share view.
    /// </summary>
    Task<bool> RecordShareViewAsync(Guid shareId, string ipAddress, string userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates password for password-protected share (for management).
    /// </summary>
    Task<ShareAccessInfoDto> ValidateSharePasswordAsync(SharePasswordValidationDto validationDto, string ipAddress,
        string userAgent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user's share view trend data.
    /// </summary>
    Task<DocumentShareTrendDataDto> GetUserShareViewTrendAsync(string userId, int monthCount = 6, CancellationToken cancellationToken = default);

}