using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Repositories;
using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Persistence.Repositories;

public class ShareApprovalRequestRepository : Repository<ShareApprovalRequest>, IShareApprovalRequestRepository
{
    public ShareApprovalRequestRepository(ApplicationDbContext context, ILogger<ShareApprovalRequestRepository> logger)
        : base(context, logger)
    {
    }

    // Override base GetByIdAsync to include navigation properties
    public override async Task<ShareApprovalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.DocumentShare)
                .ThenInclude(s => s.Document)
                    .ThenInclude(d => d.Category)
            .Include(r => r.ApprovalPolicy)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovalDecisions)
                .ThenInclude(d => d.ApproverUser)
            .Include(r => r.ApprovalHistory)
                .ThenInclude(h => h.ActionByUser)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    public async Task<ShareApprovalRequest?> GetByDocumentShareIdAsync(Guid documentShareId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.DocumentShare)
                .ThenInclude(s => s.Document)
                    .ThenInclude(d => d.Category)
            .Include(r => r.ApprovalPolicy)
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovalDecisions)
                .ThenInclude(d => d.ApproverUser)
            .Include(r => r.ApprovalHistory)
                .ThenInclude(h => h.ActionByUser)
            .FirstOrDefaultAsync(r => r.DocumentShareId == documentShareId, cancellationToken);
    }

    public async Task<IEnumerable<ShareApprovalRequest>> GetPendingRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.DocumentShare)
                .ThenInclude(s => s.Document)
                    .ThenInclude(d => d.Category)
            .Include(r => r.ApprovalPolicy)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == ApprovalStatus.Pending && r.ExpiresAt > DateTime.UtcNow)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShareApprovalRequest>> GetPendingRequestsForUserAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.DocumentShare)
                .ThenInclude(s => s.Document)
                    .ThenInclude(d => d.Category)
            .Include(r => r.ApprovalPolicy)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == ApprovalStatus.Pending &&
                        r.ExpiresAt > DateTime.UtcNow &&
                        (r.AssignedApprovers == null || r.AssignedApprovers.Contains(userId)))
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShareApprovalRequest>> GetByStatusAsync(ApprovalStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.DocumentShare)
                .ThenInclude(s => s.Document)
                    .ThenInclude(d => d.Category)
            .Include(r => r.ApprovalPolicy)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShareApprovalRequest>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.DocumentShare)
                .ThenInclude(s => s.Document)
                    .ThenInclude(d => d.Category)
            .Include(r => r.ApprovalPolicy)
            .Include(r => r.RequestedByUser)
            .Where(r => r.CreatedAt >= fromDate && r.CreatedAt <= toDate)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShareApprovalRequest>> GetExpiredRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.DocumentShare)
                .ThenInclude(s => s.Document)
                    .ThenInclude(d => d.Category)
            .Include(r => r.ApprovalPolicy)
            .Include(r => r.RequestedByUser)
            .Where(r => r.Status == ApprovalStatus.Pending && r.ExpiresAt <= DateTime.UtcNow)
            .OrderBy(r => r.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ShareApprovalRequest>> GetByRequestedByUserAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.DocumentShare)
                .ThenInclude(s => s.Document)
                    .ThenInclude(d => d.Category)
            .Include(r => r.ApprovalPolicy)
            .Include(r => r.RequestedByUser)
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
