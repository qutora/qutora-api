using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// DTO used for updating API Keys
/// </summary>
public class UpdateApiKeyDto
{
    /// <summary>
    /// Name of the API Key
    /// </summary>
    [MaxLength(100, ErrorMessage = "API Key name can be at most 100 characters.")]
    public string? Name { get; set; }

    /// <summary>
    /// Expiration date (if null, never expires)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Permission level (ReadOnly, ReadWrite, FullAccess)
    /// </summary>
    public ApiKeyPermission? Permission { get; set; }

    /// <summary>
    /// Storage provider IDs that have access permission
    /// </summary>
    public List<Guid>? AllowedProviderIds { get; set; }
}