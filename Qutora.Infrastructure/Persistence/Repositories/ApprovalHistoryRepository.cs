using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Repositories;

namespace Qutora.Infrastructure.Persistence.Repositories;

public class ApprovalHistoryRepository : Repository<ApprovalHistory>, IApprovalHistoryRepository
{
    public ApprovalHistoryRepository(ApplicationDbContext context, ILogger<ApprovalHistoryRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<ApprovalHistory>> GetByApprovalRequestIdAsync(Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(h => h.ActionByUser)
            .Include(h => h.ShareApprovalRequest)
            .Where(h => h.ShareApprovalRequestId == approvalRequestId)
            .OrderBy(h => h.ActionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApprovalHistory>> GetByUserAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(h => h.ActionByUser)
            .Include(h => h.ShareApprovalRequest)
            .ThenInclude(r => r.DocumentShare)
            .ThenInclude(s => s.Document)
            .Where(h => h.ActionByUserId == userId)
            .OrderByDescending(h => h.ActionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApprovalHistory>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(h => h.ActionByUser)
            .Include(h => h.ShareApprovalRequest)
            .ThenInclude(r => r.DocumentShare)
            .ThenInclude(s => s.Document)
            .Where(h => h.ActionDate >= fromDate && h.ActionDate <= toDate)
            .OrderByDescending(h => h.ActionDate)
            .ToListAsync(cancellationToken);
    }
}
