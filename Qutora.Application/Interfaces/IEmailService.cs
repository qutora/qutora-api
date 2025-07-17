using Qutora.Shared.DTOs.Email;

namespace Qutora.Application.Interfaces;

public interface IEmailService
{
    // Settings management
    Task<EmailSettingsDto?> GetSettingsAsync();
    Task<EmailSettingsDto> UpdateSettingsAsync(UpdateEmailSettingsDto dto);
    Task<bool> SendTestEmailAsync(string toEmail);
    Task<bool> SendTestTemplateEmailAsync(Guid templateId, string toEmail);
    
    // Email sending
    Task<bool> SendApprovalRequestNotificationAsync(string toEmail, string approverName, string documentName, string requesterName, string requestReason, string shareCode, DateTime expiresAt, string documentCategory = "", string fileSize = "", string policyName = "");
    Task<bool> SendApprovalDecisionNotificationAsync(string toEmail, string recipientName, string documentName, string decision, string decisionReason, string shareCode, string shareUrl);
    Task<bool> SendDocumentShareNotificationAsync(string toEmail, string recipientName, string documentName, string sharedBy, string shareCode, string shareUrl, DateTime expiresAt, string customMessage = "");
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task<bool> SendEmailAsync(List<string> toEmails, string subject, string htmlBody);
    
    // Template management
    Task<List<EmailTemplateDto>> GetTemplatesAsync();
    Task<EmailTemplateDto?> GetTemplateByIdAsync(Guid id);
    Task<EmailTemplateDto> CreateTemplateAsync(CreateEmailTemplateDto dto);
    Task<EmailTemplateDto> UpdateTemplateAsync(Guid id, UpdateEmailTemplateDto dto);
    Task<bool> DeleteTemplateAsync(Guid id);
} 