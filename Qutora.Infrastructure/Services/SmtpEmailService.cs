using System.Net;
using System.Net.Mail;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Application.Security;
using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Email;
using Qutora.Shared.Enums;
using Qutora.Shared.Models;

namespace Qutora.Infrastructure.Services;

public class SmtpEmailService(
    IUnitOfWork unitOfWork,
    ILogger<SmtpEmailService> logger,
    ISensitiveDataProtector dataProtector,
    IOptions<PublicViewerSettings> publicViewerOptions,
    IConfiguration configuration,
    IEmailTemplateRepository emailTemplateRepository,
    IApprovalPolicyRepository approvalPolicyRepository,
    IOptions<EmailSampleDataSettings> sampleDataOptions)
    : IEmailService
{
    private readonly PublicViewerSettings _publicViewerSettings = publicViewerOptions.Value;
    private readonly IConfiguration _configuration = configuration;
    private readonly IApprovalPolicyRepository _approvalPolicyRepository = approvalPolicyRepository;
    private readonly EmailSampleDataSettings _sampleDataSettings = sampleDataOptions.Value;

    public async Task<EmailSettingsDto?> GetSettingsAsync()
    {
        var settings = await unitOfWork.EmailSettings.GetCurrentAsync();
        return settings?.Adapt<EmailSettingsDto>();
    }

    public async Task<EmailSettingsDto> UpdateSettingsAsync(UpdateEmailSettingsDto dto)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var settings = await unitOfWork.EmailSettings.GetCurrentAsync();
            
            if (settings == null)
            {
                settings = new EmailSettings
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                };
                await unitOfWork.EmailSettings.AddAsync(settings);
            }

            dto.Adapt(settings);
            settings.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(dto.SmtpPassword))
            {
                settings.SmtpPassword = dataProtector.Protect(dto.SmtpPassword);
            }

            return settings.Adapt<EmailSettingsDto>();
        });
    }

    public async Task<bool> SendTestEmailAsync(string toEmail)
    {
        var settings = await unitOfWork.EmailSettings.GetCurrentAsync();
        if (settings == null || !settings.IsEnabled)
        {
            logger.LogWarning("Email settings not configured or disabled");
            return false;
        }

        try
        {
            var subject = "Test Email from Qutora System";
            var body = $@"
                <html>
                <body>
                    <h2>Test Email</h2>
                    <p>This is a test email sent from Qutora document management system.</p>
                    <p>If you received this email, your email configuration is working correctly.</p>
                    <p>Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                </body>
                </html>";

            var success = await SendEmailAsync(toEmail, subject, body);
            
            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.EmailSettings.UpdateTestEmailStatusAsync(
                    settings.Id, 
                    DateTime.UtcNow, 
                    success ? "Success" : "Failed");
            });
            
            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send test email");
            
            await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.EmailSettings.UpdateTestEmailStatusAsync(
                    settings.Id, 
                    DateTime.UtcNow, 
                    $"Error: {ex.Message}");
            });
            
            return false;
        }
    }

    public async Task<bool> SendTestTemplateEmailAsync(Guid templateId, string toEmail)
    {
        try
        {
            var template = await emailTemplateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                logger.LogWarning("Template not found with ID: {TemplateId}", templateId);
                return false;
            }

            var sampleVariables = CreateSampleVariables(template.TemplateType);
        
            logger.LogInformation("Sample variables for template {TemplateType}: {Variables}", 
                template.TemplateType, string.Join(", ", sampleVariables.Select(kv => $"{kv.Key}={kv.Value}")));
            
            var processedSubject = ReplaceVariables($"[TEST] {template.Subject}", sampleVariables);
            var processedBody = ReplaceVariables(template.Body, sampleVariables);
        
            logger.LogInformation("Original template body length: {Length}, Processed body length: {ProcessedLength}", 
                template.Body?.Length ?? 0, processedBody?.Length ?? 0);

            return await SendEmailAsync(toEmail, processedSubject, processedBody);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending test template email for template {TemplateId}", templateId);
            return false;
        }
    }

    private Dictionary<string, string> CreateSampleVariables(EmailTemplateType templateType)
    {
        return templateType switch
        {
            EmailTemplateType.ApprovalRequest => new Dictionary<string, string>
            {
                ["ApproverName"] = "Sarah Johnson",
                ["RequesterName"] = _sampleDataSettings.ApprovalRequest.RequesterName,
                ["DocumentName"] = _sampleDataSettings.ApprovalRequest.DocumentName,
                ["PolicyName"] = _sampleDataSettings.ApprovalRequest.PolicyName,
                ["RequestReason"] = _sampleDataSettings.ApprovalRequest.RequestReason,
                ["ApproveUrl"] = _sampleDataSettings.ApprovalRequest.ApproveUrl,
                ["RejectUrl"] = _sampleDataSettings.ApprovalRequest.RejectUrl,
                ["ViewUrl"] = _sampleDataSettings.ApprovalRequest.ViewUrl,
                ["ShareUrl"] = _sampleDataSettings.ApprovalRequest.ShareUrl,
                ["ShareCode"] = "ABC123",
                ["ExpiresAt"] = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd HH:mm:ss"),
                ["ExpirationDate"] = DateTime.Now.AddDays(7).ToString("dd.MM.yyyy HH:mm"),
                ["RequestDate"] = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                ["DocumentCategory"] = "Project Documents",
                ["FileSize"] = "2.5 MB"
            },
            EmailTemplateType.ApprovalDecision => new Dictionary<string, string>
            {
                ["RecipientName"] = "John Smith",
                ["ApproverName"] = _sampleDataSettings.ApprovalDecision.ApproverName,
                ["Decision"] = _sampleDataSettings.ApprovalDecision.Decision,
                ["DecisionReason"] = _sampleDataSettings.ApprovalDecision.Comments,
                ["Comments"] = _sampleDataSettings.ApprovalDecision.Comments,
                ["DocumentName"] = _sampleDataSettings.ApprovalDecision.DocumentName,
                ["ViewUrl"] = _sampleDataSettings.ApprovalDecision.ViewUrl,
                ["ShareUrl"] = _sampleDataSettings.ApprovalDecision.ViewUrl,
                ["ShareCode"] = "DEF456"
            },
            EmailTemplateType.DocumentShareNotification => new Dictionary<string, string>
            {
                ["SenderName"] = _sampleDataSettings.DocumentShareNotification.SenderName,
                ["SharedBy"] = _sampleDataSettings.DocumentShareNotification.SenderName,
                ["DocumentName"] = _sampleDataSettings.DocumentShareNotification.DocumentName,
                ["RecipientName"] = _sampleDataSettings.DocumentShareNotification.RecipientName,
                ["Message"] = _sampleDataSettings.DocumentShareNotification.Message,
                ["ShareMessage"] = _sampleDataSettings.DocumentShareNotification.Message,
                ["CustomMessage"] = $"<p><strong>Message:</strong> {_sampleDataSettings.DocumentShareNotification.Message}</p>",
                ["ShareUrl"] = _sampleDataSettings.DocumentShareNotification.ShareUrl,
                ["ViewUrl"] = _sampleDataSettings.DocumentShareNotification.ViewUrl,
                ["ShareCode"] = "GHI789",
                ["ExpiresAt"] = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd HH:mm:ss"),
                ["ExpirationDate"] = DateTime.Now.AddDays(30).ToString("dd.MM.yyyy HH:mm")
            },
            _ => new Dictionary<string, string>()
        };
    }

    public async Task<bool> SendApprovalRequestNotificationAsync(
        string toEmail, 
        string approverName,
        string documentName, 
        string requesterName, 
        string requestReason, 
        string shareCode, 
        DateTime expiresAt,
        string documentCategory = "",
        string fileSize = "",
        string policyName = "")
    {
        logger.LogInformation("Attempting to send approval request email for document {DocumentName}", documentName);
        
        var template = await unitOfWork.EmailTemplates.GetByTemplateTypeAsync(Qutora.Shared.Enums.EmailTemplateType.ApprovalRequest);
        if (template == null || !template.IsActive)
        {
            logger.LogWarning("Approval request email template not found or inactive. Please create the template in admin panel.");
            return false;
        }
        
        logger.LogInformation("Found approval request template: {TemplateType}, Active: {IsActive}", template.TemplateType, template.IsActive);

        var shareUrl = BuildPublicViewerUrl(shareCode);
        
        var variables = new Dictionary<string, string>
        {
            {"ApproverName", approverName},
            {"DocumentName", documentName},
            {"RequesterName", requesterName},
            {"RequestReason", requestReason},
            {"ShareCode", shareCode},
            {"ShareUrl", shareUrl},
            {"ApproveUrl", shareUrl},
            {"RejectUrl", shareUrl}, 
            {"ViewUrl", shareUrl},
            {"PolicyName", string.IsNullOrEmpty(policyName) ? "System Policy" : policyName},
            {"ExpiresAt", expiresAt.ToString("yyyy-MM-dd HH:mm:ss")},
            {"ExpirationDate", expiresAt.ToString("dd.MM.yyyy HH:mm")},
            {"RequestDate", DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")},
            {"DocumentCategory", documentCategory},
            {"FileSize", fileSize}
        };

        var subject = ReplaceVariables(template.Subject, variables);
        var body = ReplaceVariables(template.Body, variables);

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendApprovalDecisionNotificationAsync(
        string toEmail, 
        string recipientName,
        string documentName, 
        string decision, 
        string decisionReason, 
        string shareCode, 
        string shareUrl)
    {
        var template = await unitOfWork.EmailTemplates.GetByTemplateTypeAsync(Qutora.Shared.Enums.EmailTemplateType.ApprovalDecision);
        if (template == null || !template.IsActive)
        {
            logger.LogWarning("Approval decision email template not found or inactive");
            return false;
        }

        // Use PublicViewer URL instead of passed shareUrl  
        var publicViewerUrl = BuildPublicViewerUrl(shareCode);
        
        var variables = new Dictionary<string, string>
        {
            {"RecipientName", recipientName},
            {"DocumentName", documentName},
            {"Decision", decision},
            {"DecisionReason", decisionReason},
            {"ShareCode", shareCode},
            {"ShareUrl", publicViewerUrl}
        };

        var subject = ReplaceVariables(template.Subject, variables);
        var body = ReplaceVariables(template.Body, variables);

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendDocumentShareNotificationAsync(
        string toEmail, 
        string recipientName,
        string documentName, 
        string sharedBy, 
        string shareCode, 
        string shareUrl, 
        DateTime expiresAt, 
        string customMessage = "")
    {
        var template = await unitOfWork.EmailTemplates.GetByTemplateTypeAsync(Qutora.Shared.Enums.EmailTemplateType.DocumentShareNotification);
        if (template == null || !template.IsActive)
        {
            logger.LogWarning("Document share email template not found or inactive");
            return false;
        }

        // Use PublicViewer URL instead of passed shareUrl
        var publicViewerUrl = BuildPublicViewerUrl(shareCode);
        
        var variables = new Dictionary<string, string>
        {
            {"RecipientName", recipientName},
            {"DocumentName", documentName},
            {"SharedBy", sharedBy},
            {"SenderName", sharedBy},
            {"ShareCode", shareCode},
            {"ShareUrl", publicViewerUrl},
            {"ExpiresAt", expiresAt.ToString("yyyy-MM-dd HH:mm:ss")},
            {"ExpirationDate", expiresAt.ToString("dd.MM.yyyy HH:mm")},
            {"ShareMessage", customMessage},
            {"CustomMessage", string.IsNullOrEmpty(customMessage) ? "" : $"<p><strong>Message:</strong> {customMessage}</p>"}
        };

        var subject = ReplaceVariables(template.Subject, variables);
        var body = ReplaceVariables(template.Body, variables);

        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var settings = await unitOfWork.EmailSettings.GetCurrentAsync();
        if (settings == null || !settings.IsEnabled)
        {
            logger.LogWarning("Email settings not configured or disabled");
            return false;
        }

        try
        {
            // SECURITY: Only log non-sensitive connection details
            logger.LogInformation("Attempting to send email via SMTP: {Server}:{Port}, SSL: {SSL}", 
                settings.SmtpServer, settings.SmtpPort, settings.UseSsl);
                
            using var client = new SmtpClient(settings.SmtpServer, settings.SmtpPort);
            client.EnableSsl = settings.UseSsl;
            
            if (!string.IsNullOrEmpty(settings.SmtpUsername) && !string.IsNullOrEmpty(settings.SmtpPassword))
            {
                var password = dataProtector.Unprotect(settings.SmtpPassword);
                client.Credentials = new NetworkCredential(settings.SmtpUsername, password);
                // SECURITY: Do not log username or any credential information
                logger.LogDebug("SMTP credentials configured successfully");
            }
            else
            {
                logger.LogWarning("SMTP credentials not configured");
            }

            using var message = new MailMessage();
            message.From = new MailAddress(settings.FromEmail, settings.FromName);
            message.To.Add(toEmail);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            if (!string.IsNullOrEmpty(settings.ReplyToEmail))
            {
                message.ReplyToList.Add(settings.ReplyToEmail);
            }

            await client.SendMailAsync(message);
            
            logger.LogInformation("Email sent successfully with subject: {Subject}", subject);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email with subject: {Subject}", subject);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(List<string> toEmails, string subject, string htmlBody)
    {
        var results = new List<bool>();
        
        foreach (var email in toEmails)
        {
            var result = await SendEmailAsync(email, subject, htmlBody);
            results.Add(result);
        }

        return results.All(r => r);
    }

    public async Task<List<EmailTemplateDto>> GetTemplatesAsync()
    {
        var templates = await unitOfWork.EmailTemplates.GetActiveTemplatesAsync();
        return templates.Adapt<List<EmailTemplateDto>>();
    }

    public async Task<EmailTemplateDto?> GetTemplateByIdAsync(Guid id)
    {
        var template = await unitOfWork.EmailTemplates.GetByIdAsync(id);
        return template?.Adapt<EmailTemplateDto>();
    }

    public async Task<EmailTemplateDto> CreateTemplateAsync(CreateEmailTemplateDto dto)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            // Check if template type already exists
            var exists = await unitOfWork.EmailTemplates.ExistsByTemplateTypeAsync(dto.TemplateType);
            if (exists)
                throw new ArgumentException($"Template with type '{dto.TemplateType}' already exists");

            var template = new EmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateType = dto.TemplateType,
                Description = dto.Description,
                Subject = dto.Subject,
                Body = dto.Body,
                AvailableVariables = dto.AvailableVariables,
                IsActive = dto.IsActive,
                IsSystem = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await unitOfWork.EmailTemplates.AddAsync(template);

            return template.Adapt<EmailTemplateDto>();
        });
    }

    public async Task<EmailTemplateDto> UpdateTemplateAsync(Guid id, UpdateEmailTemplateDto dto)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var template = await unitOfWork.EmailTemplates.GetByIdAsync(id);
            if (template == null)
                throw new ArgumentException("Template not found");

            if (template.IsSystem)
                throw new InvalidOperationException("System templates cannot be modified");

            // Check if template type already exists (excluding current template)
            var exists = await unitOfWork.EmailTemplates.ExistsByTemplateTypeAsync(dto.TemplateType, id);
            if (exists)
                throw new ArgumentException($"Template with type '{dto.TemplateType}' already exists");

            template.TemplateType = dto.TemplateType;
            template.Description = dto.Description;
            template.Subject = dto.Subject;
            template.Body = dto.Body;
            template.AvailableVariables = dto.AvailableVariables;
            template.IsActive = dto.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            return template.Adapt<EmailTemplateDto>();
        });
    }

    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var template = await unitOfWork.EmailTemplates.GetByIdAsync(id);
            if (template == null)
                return false;

            if (template.IsSystem)
                throw new InvalidOperationException("System templates cannot be deleted");

            unitOfWork.EmailTemplates.Remove(template);

            return true;
        });
    }

    private string BuildPublicViewerUrl(string shareCode)
    {
        if (string.IsNullOrWhiteSpace(_publicViewerSettings.BaseUrl))
        {
            logger.LogWarning("PublicViewer BaseUrl not configured. Using fallback URL format.");
            return $"/document/{shareCode}";
        }
        
        var baseUrl = _publicViewerSettings.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/document/{shareCode}";
    }
    
    private static string ReplaceVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;
        
        foreach (var variable in variables)
        {
            result = result.Replace($"{{{variable.Key}}}", variable.Value);
        }

        return result;
    }
} 