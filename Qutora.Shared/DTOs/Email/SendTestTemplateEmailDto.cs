using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs.Email;

public class SendTestTemplateEmailDto
{
    [Required]
    public Guid TemplateId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string ToEmail { get; set; } = string.Empty;
} 