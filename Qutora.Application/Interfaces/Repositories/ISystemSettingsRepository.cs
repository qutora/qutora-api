using Qutora.Domain.Entities;

namespace Qutora.Application.Interfaces.Repositories;

public interface ISystemSettingsRepository : IRepository<SystemSettings>
{
    Task<SystemSettings?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
} 