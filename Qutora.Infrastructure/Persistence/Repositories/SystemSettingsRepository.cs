using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;

namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// SystemSettings repository implementation following Clean Architecture standards
/// </summary>
public class SystemSettingsRepository : Repository<SystemSettings>, ISystemSettingsRepository
{
    public SystemSettingsRepository(ApplicationDbContext context, ILogger<SystemSettingsRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<SystemSettings?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(cancellationToken);
    }
} 