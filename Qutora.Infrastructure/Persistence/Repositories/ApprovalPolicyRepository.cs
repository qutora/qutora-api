using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Domain.Entities;

using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Approval;

namespace Qutora.Infrastructure.Persistence.Repositories;

public class ApprovalPolicyRepository : Repository<ApprovalPolicy>, IApprovalPolicyRepository
{
    public ApprovalPolicyRepository(ApplicationDbContext context, ILogger<ApprovalPolicyRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<ApprovalPolicy>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.CreatedByUser)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApprovalPolicy?> GetDefaultForcedPolicyAsync(CancellationToken cancellationToken = default)
    {
        var globalPolicy = await _dbSet
            .Include(p => p.CreatedByUser)
            .Where(p => p.IsActive && p.RequireApproval && p.Name == "Global System Policy")
            .FirstOrDefaultAsync(cancellationToken);

        if (globalPolicy != null)
            return globalPolicy;

        return await _dbSet
            .Include(p => p.CreatedByUser)
            .Where(p => p.IsActive && p.RequireApproval && p.Name.Contains("Default"))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ApprovalPolicy?> GetGlobalSystemPolicyAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.CreatedByUser)
            .Where(p => p.Name == "Global System Policy")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsGlobalSystemPolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _dbSet
            .Where(p => p.Id == policyId && p.Name == "Global System Policy")
            .FirstOrDefaultAsync(cancellationToken);

        return policy != null;
    }

    public async Task<IEnumerable<ApprovalPolicy>> GetByCreatedByUserAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.CreatedByUser)
            .Where(p => p.CreatedBy == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ApprovalPolicy>> GetByFiltersAsync(
        bool? isActive = null,
        bool? requireApproval = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Include(p => p.CreatedByUser).AsQueryable();

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (requireApproval.HasValue)
            query = query.Where(p => p.RequireApproval == requireApproval.Value);

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedDto<ApprovalPolicy>> GetPagedAsync(ApprovalPolicyQueryDto query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ApprovalPolicy> queryable = _dbSet.Include(p => p.CreatedByUser);

        if (query.IsActive.HasValue)
            queryable = queryable.Where(x => x.IsActive == query.IsActive.Value);

        if (!string.IsNullOrEmpty(query.Name))
            queryable = queryable.Where(x => x.Name.Contains(query.Name));


        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedDto<ApprovalPolicy>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }
}
