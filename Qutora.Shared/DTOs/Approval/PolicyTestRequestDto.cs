using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Approval;

public class PolicyTestRequestDto
{
    [Required] public Guid DocumentId { get; set; }

    [Required] public Guid UserId { get; set; }

    public Guid? ApiKeyId { get; set; }

    public int? FileSizeMB { get; set; }

    public string? FileExtension { get; set; }

    public int? MaxViews { get; set; }

    public int? DurationDays { get; set; }

    public string? DocumentName { get; set; }

    public Guid? CategoryId { get; set; }

    public string? RequestedByUserId { get; set; }

    public ShareType? ShareType { get; set; }
}