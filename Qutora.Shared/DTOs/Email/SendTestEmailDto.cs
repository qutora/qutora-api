using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs.Email;

public class SendTestEmailDto
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string ToEmail { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Subject { get; set; }

    [StringLength(1000)]
    public string? Message { get; set; }
} 