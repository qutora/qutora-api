using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;


namespace Qutora.Infrastructure.Persistence.Repositories;

public class ApprovalDecisionRepository : Repository<ApprovalDecision>, IApprovalDecisionRepository
{
    public ApprovalDecisionRepository(ApplicationDbContext context, ILogger<ApprovalDecisionRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<ApprovalDecision>> GetByApprovalRequestIdAsync(Guid approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.ApproverUser)
            .Include(d => d.ShareApprovalRequest)
            .Where(d => d.ShareApprovalRequestId == approvalRequestId)
            .OrderBy(d => d.ApprovedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApprovalDecision?> GetByApprovalRequestAndUserAsync(Guid approvalRequestId, string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.ApproverUser)
            .Include(d => d.ShareApprovalRequest)
            .FirstOrDefaultAsync(d => d.ShareApprovalRequestId == approvalRequestId && d.ApproverUserId == userId,
                cancellationToken);
    }

    public async Task<IEnumerable<ApprovalDecision>> GetByApproverUserAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(d => d.ApproverUser)
            .Include(d => d.ShareApprovalRequest)
            .ThenInclude(r => r.DocumentShare)
            .ThenInclude(s => s.Document)
            .Where(d => d.ApproverUserId == userId)
            .OrderByDescending(d => d.ApprovedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasUserAlreadyDecidedAsync(Guid approvalRequestId, string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(d => d.ShareApprovalRequestId == approvalRequestId && d.ApproverUserId == userId,
                cancellationToken);
    }
}
