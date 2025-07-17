using Microsoft.EntityFrameworkCore;
using Qutora.Domain.Entities;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces.Repositories;


namespace Qutora.Infrastructure.Persistence.Repositories;

/// <summary>
/// Category repository implementation following Clean Architecture standards
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context, ILogger<CategoryRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Gets all categories excluding deleted ones
    /// </summary>
    public override async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Documents.Where(d => !d.IsDeleted))
            .Include(c => c.ParentCategory)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets category by ID excluding deleted ones
    /// </summary>
    public override async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Gets root categories
    /// </summary>
    public async Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Documents.Where(d => !d.IsDeleted))
            .Where(c => c.ParentCategoryId == null && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets subcategories
    /// </summary>
    public async Task<IEnumerable<Category>> GetSubCategoriesAsync(Guid parentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Documents.Where(d => !d.IsDeleted))
            .Where(c => c.ParentCategoryId == parentId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if category can be deleted
    /// </summary>
    public async Task<bool> CanDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var hasSubCategories = await _dbSet
            .AnyAsync(c => c.ParentCategoryId == id && !c.IsDeleted, cancellationToken);

        if (hasSubCategories)
            return false;

        var hasDocuments = await _context.Documents
            .AnyAsync(d => d.CategoryId == id && !d.IsDeleted, cancellationToken);

        return !hasDocuments;
    }

    /// <summary>
    /// Gets category by ID with details
    /// </summary>
    public async Task<Category?> GetByIdWithDetailsAsync(Guid id, bool includeSubCategories = true,
        bool includeDocuments = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (includeSubCategories) query = query.Include(c => c.SubCategories.Where(sc => !sc.IsDeleted));

        if (includeDocuments) query = query.Include(c => c.Documents.Where(d => !d.IsDeleted));

        return await query
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Gets category tree
    /// </summary>
    public async Task<IEnumerable<Category>> GetCategoryTreeAsync(CancellationToken cancellationToken = default)
    {
        var allCategories = await _dbSet
            .Include(c => c.SubCategories)
            .Where(c => !c.IsDeleted)
            .ToListAsync(cancellationToken);

        return allCategories.Where(c => c.ParentCategoryId == null).ToList();
    }

    /// <summary>
    /// Checks if category exists
    /// </summary>
    public override async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// Checks if category name is in use
    /// </summary>
    public async Task<bool> IsNameInUseAsync(string name, Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(c => c.Name.ToLower() == name.ToLower() && !c.IsDeleted);

        if (excludeId.HasValue) query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Loads categories recursively - use with caution for performance
    /// </summary>
    public async Task<Category?> GetCategoryWithFullHierarchyAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.ParentCategory)
            .ThenInclude(c => c != null ? c.ParentCategory : null)
            .Include(c => c.SubCategories.Where(sc => !sc.IsDeleted))
            .ThenInclude(c => c.SubCategories.Where(sc => !sc.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }
}
