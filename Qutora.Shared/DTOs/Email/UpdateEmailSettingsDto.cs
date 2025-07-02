using System.ComponentModel.DataAnnotations;

namespace Qutora.Shared.DTOs.Email;

public class UpdateEmailSettingsDto
{
    public bool? IsEnabled { get; set; }

    [StringLength(200)]
    public string? SmtpServer { get; set; }

    [Range(1, 65535)]
    public int? SmtpPort { get; set; }

    public bool? UseSsl { get; set; }

    [StringLength(200)]
    public string? SmtpUsername { get; set; }

    [StringLength(500)]
    public string? SmtpPassword { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? FromEmail { get; set; }

    [StringLength(200)]
    public string? FromName { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? ReplyToEmail { get; set; }


} 