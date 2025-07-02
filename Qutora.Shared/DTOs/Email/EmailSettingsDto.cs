namespace Qutora.Shared.DTOs.Email;

public class EmailSettingsDto
{
    public Guid Id { get; set; }
    public bool IsEnabled { get; set; }
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool UseSsl { get; set; }
    public string? SmtpUsername { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? ReplyToEmail { get; set; }

    public DateTime? LastTestEmailSentAt { get; set; }
    public string? LastTestEmailStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
} 