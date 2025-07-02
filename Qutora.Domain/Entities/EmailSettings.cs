using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;

namespace Qutora.Domain.Entities;

public class EmailSettings : BaseEntity
{
    /// <summary>
    /// Is email system enabled?
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// SMTP server hostname
    /// </summary>
    [Required]
    [StringLength(200)]
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Use SSL/TLS encryption
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// SMTP username
    /// </summary>
    [StringLength(200)]
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password (encrypted)
    /// </summary>
    [StringLength(500)]
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// From email address
    /// </summary>
    [Required]
    [StringLength(200)]
    [EmailAddress]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    [StringLength(200)]
    public string FromName { get; set; } = "Qutora System";

    /// <summary>
    /// Reply-to email address
    /// </summary>
    [StringLength(200)]
    [EmailAddress]
    public string? ReplyToEmail { get; set; }



    /// <summary>
    /// Test email sent successfully at
    /// </summary>
    public DateTime? LastTestEmailSentAt { get; set; }

    /// <summary>
    /// Last test email status
    /// </summary>
    [StringLength(500)]
    public string? LastTestEmailStatus { get; set; }
} 