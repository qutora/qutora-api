using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

public interface IEmailSettingsRepository : IRepository<EmailSettings>
{
    /// <summary>
    /// Get current email settings (should be only one record)
    /// </summary>
    Task<EmailSettings?> GetCurrentAsync();

    /// <summary>
    /// Update test email status
    /// </summary>
    Task UpdateTestEmailStatusAsync(Guid id, DateTime testDate, string status);
} 