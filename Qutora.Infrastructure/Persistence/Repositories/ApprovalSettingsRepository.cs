using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Repositories;

namespace Qutora.Infrastructure.Persistence.Repositories;

public class ApprovalSettingsRepository : Repository<ApprovalSettings>, IApprovalSettingsRepository
{
    public ApprovalSettingsRepository(ApplicationDbContext context, ILogger<ApprovalSettingsRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<ApprovalSettings?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ApprovalSettings> GetOrCreateDefaultAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetCurrentAsync(cancellationToken);

        if (settings == null)
        {
            settings = new ApprovalSettings
            {
                Id = Guid.NewGuid(),
                ForceApprovalForAll = false,
                CreatedAt = DateTime.UtcNow
            };

            await AddAsync(settings, cancellationToken);
        }

        return settings;
    }
}
