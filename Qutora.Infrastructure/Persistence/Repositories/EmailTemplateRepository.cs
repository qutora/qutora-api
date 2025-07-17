using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;

using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Persistence.Repositories;

public class EmailTemplateRepository : Repository<EmailTemplate>, IEmailTemplateRepository
{
    public EmailTemplateRepository(ApplicationDbContext context, ILogger<EmailTemplateRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<List<EmailTemplate>> GetActiveTemplatesAsync()
    {
        return await _context.EmailTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.TemplateType)
            .ToListAsync();
    }

    public async Task<List<EmailTemplate>> GetSystemTemplatesAsync()
    {
        return await _context.EmailTemplates
            .Where(t => t.IsSystem)
            .OrderBy(t => t.TemplateType)
            .ToListAsync();
    }

    public async Task<EmailTemplate?> GetByTemplateTypeAsync(EmailTemplateType templateType)
    {
        return await _context.EmailTemplates
            .FirstOrDefaultAsync(t => t.TemplateType == templateType && t.IsActive);
    }

    public async Task<bool> ExistsByTemplateTypeAsync(EmailTemplateType templateType, Guid? excludeId = null)
    {
        var query = _context.EmailTemplates.Where(t => t.TemplateType == templateType);
        
        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }
        
        return await query.AnyAsync();
    }
} 