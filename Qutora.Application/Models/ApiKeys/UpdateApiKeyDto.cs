using System.ComponentModel.DataAnnotations;

namespace Qutora.Application.Models.ApiKeys;

public class UpdateApiKeyDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    [RegularExpression("ReadOnly|ReadWrite|FullAccess",
        ErrorMessage = "Permission must be one of: ReadOnly, ReadWrite, FullAccess")]
    public string? Permission { get; set; }

    public ICollection<Guid>? AllowedProviderIds { get; set; }
}