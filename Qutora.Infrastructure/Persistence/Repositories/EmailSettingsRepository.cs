using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Repositories;

namespace Qutora.Infrastructure.Persistence.Repositories;

public class EmailSettingsRepository : Repository<EmailSettings>, IEmailSettingsRepository
{
    public EmailSettingsRepository(ApplicationDbContext context, ILogger<EmailSettingsRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<EmailSettings?> GetCurrentAsync()
    {
        return await _context.EmailSettings
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task UpdateTestEmailStatusAsync(Guid id, DateTime testDate, string status)
    {
        var settings = await GetByIdAsync(id);
        if (settings != null)
        {
            settings.LastTestEmailSentAt = testDate;
            settings.LastTestEmailStatus = status;
            settings.UpdatedAt = DateTime.UtcNow;
            
            _context.EmailSettings.Update(settings);
        }
    }
} 