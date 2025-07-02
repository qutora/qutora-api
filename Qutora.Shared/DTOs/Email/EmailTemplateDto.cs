using Qutora.Shared.Enums;

namespace Qutora.Shared.DTOs.Email;

public class EmailTemplateDto
{
    public Guid Id { get; set; }
    public EmailTemplateType TemplateType { get; set; }
    public string? Description { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? AvailableVariables { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
} 