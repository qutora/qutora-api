using Qutora.Shared.DTOs;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for category operations.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets category by ID.
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category or null</returns>
    Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category list</returns>
    Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets root categories (without parent category).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Root categories list</returns>
    Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sub-categories of a specific category.
    /// </summary>
    /// <param name="parentId">Parent category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sub-categories list</returns>
    Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="createCategoryDto">Category to be created</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category</returns>
    Task<CategoryDto> CreateAsync(CreateCategoryDto createCategoryDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="updateCategoryDto">Category to be updated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category</returns>
    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto updateCategoryDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category (soft delete).
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation is successful</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}