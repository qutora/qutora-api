using System.ComponentModel.DataAnnotations;
using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Email;

public class UpdateEmailTemplateDto
{
    [Required]
    public EmailTemplateType TemplateType { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public string? AvailableVariables { get; set; }

    public bool IsActive { get; set; } = true;
} 