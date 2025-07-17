using Qutora.Domain.Entities;
using Qutora.Shared.Enums;

namespace Qutora.Application.Interfaces.Repositories;

public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    Task<EmailTemplate?> GetByTemplateTypeAsync(EmailTemplateType templateType);
    Task<List<EmailTemplate>> GetActiveTemplatesAsync();
    Task<List<EmailTemplate>> GetSystemTemplatesAsync();
    Task<bool> ExistsByTemplateTypeAsync(EmailTemplateType templateType, Guid? excludeId = null);
} 