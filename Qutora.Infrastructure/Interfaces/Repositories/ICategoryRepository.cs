using Qutora.Domain.Entities;

namespace Qutora.Infrastructure.Interfaces.Repositories;

/// <summary>
/// Category repository interface
/// Compliant with Clean Architecture standards
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Gets root categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root categories</returns>
    Task<IEnumerable<Category>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subcategories
    /// </summary>
    /// <param name="parentId">Parent category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of subcategories</returns>
    Task<IEnumerable<Category>> GetSubCategoriesAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if category can be deleted
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if can be deleted, false otherwise</returns>
    Task<bool> CanDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets category by ID with details
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="includeSubCategories">Whether to include subcategories</param>
    /// <param name="includeDocuments">Whether to include documents in the category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category or null</returns>
    Task<Category?> GetByIdWithDetailsAsync(Guid id, bool includeSubCategories = true, bool includeDocuments = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets category tree
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root categories with their subcategories</returns>
    Task<IEnumerable<Category>> GetCategoryTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if category name is in use
    /// </summary>
    /// <param name="name">Category name</param>
    /// <param name="excludeId">Category ID to exclude (for update scenarios)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if name is in use, false otherwise</returns>
    Task<bool> IsNameInUseAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads categories recursively - should be used carefully for performance
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category or null</returns>
    Task<Category?> GetCategoryWithFullHierarchyAsync(Guid id, CancellationToken cancellationToken = default);
}