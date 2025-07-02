using MapsterMapper;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Exceptions;
using Qutora.Infrastructure.Interfaces;
using Qutora.Infrastructure.Interfaces.UnitOfWork;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Services;

/// <summary>
/// Service implementation for category operations.
/// </summary>
public class CategoryService(
    IUnitOfWork unitOfWork,
    ILogger<CategoryService> logger,
    IMapper mapper)
    : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<CategoryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<CategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdWithDetailsAsync(id, true, false, cancellationToken);
            return category != null ? _mapper.Map<CategoryDto>(category) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all categories");
            throw;
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetRootCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _unitOfWork.Categories.GetRootCategoriesAsync(cancellationToken);
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving root categories");
            throw;
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetSubCategoriesAsync(Guid parentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _unitOfWork.Categories.GetSubCategoriesAsync(parentId, cancellationToken);
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subcategories for parent ID {ParentId}", parentId);
            throw;
        }
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto createCategoryDto,
        CancellationToken cancellationToken = default)
    {
        if (createCategoryDto == null)
            throw new ArgumentNullException(nameof(createCategoryDto));

        try
        {
            var nameExists = await _unitOfWork.Categories.IsNameInUseAsync(
                createCategoryDto.Name,
                null,
                cancellationToken);

            if (nameExists)
                throw new DuplicateEntityException(
                    $"Category with name '{createCategoryDto.Name}' already exists at this level");

            if (createCategoryDto.ParentCategoryId.HasValue)
            {
                var parentExists = await _unitOfWork.Categories.ExistsAsync(
                    createCategoryDto.ParentCategoryId.Value,
                    cancellationToken);

                if (!parentExists)
                    throw new EntityNotFoundException(
                        $"Parent category with ID {createCategoryDto.ParentCategoryId} not found");
            }

            var category = _mapper.Map<Category>(createCategoryDto);
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            var createdCategory = await _unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await _unitOfWork.Categories.AddAsync(category, cancellationToken);
                return category;
            }, cancellationToken);

            return _mapper.Map<CategoryDto>(createdCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category {CategoryName}", createCategoryDto.Name);
            throw;
        }
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryDto updateCategoryDto,
        CancellationToken cancellationToken = default)
    {
        if (updateCategoryDto == null)
            throw new ArgumentNullException(nameof(updateCategoryDto));

        try
        {
            var existingCategory =
                await _unitOfWork.Categories.GetByIdWithDetailsAsync(id, true, false, cancellationToken);

            if (existingCategory == null) throw new EntityNotFoundException($"Category with ID {id} not found");

            if (existingCategory.IsDeleted) throw new EntityDeletedException($"Category with ID {id} has been deleted");

            var nameExists = await _unitOfWork.Categories.IsNameInUseAsync(
                updateCategoryDto.Name,
                id,
                cancellationToken);

            if (nameExists)
                throw new DuplicateEntityException(
                    $"Category with name '{updateCategoryDto.Name}' already exists at this level");

            if (updateCategoryDto.ParentCategoryId != existingCategory.ParentCategoryId &&
                updateCategoryDto.ParentCategoryId.HasValue)
            {
                var parentExists = await _unitOfWork.Categories.ExistsAsync(
                    updateCategoryDto.ParentCategoryId.Value,
                    cancellationToken);

                if (!parentExists)
                    throw new EntityNotFoundException(
                        $"Parent category with ID {updateCategoryDto.ParentCategoryId} not found");

                if (id == updateCategoryDto.ParentCategoryId)
                    throw new InvalidOperationException("Category cannot be its own parent");

                // Check for circular reference (A->B->C->A)
                if (await WouldCreateCircularReferenceAsync(id, updateCategoryDto.ParentCategoryId.Value, cancellationToken))
                    throw new InvalidOperationException("This operation would create a circular reference in the category hierarchy");
            }

            existingCategory.Name = updateCategoryDto.Name;
            existingCategory.Description = updateCategoryDto.Description;
            existingCategory.ParentCategoryId = updateCategoryDto.ParentCategoryId;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            var updatedCategory = await _unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await _unitOfWork.Categories.UpdateAsync(existingCategory, cancellationToken);

                return existingCategory;
            }, cancellationToken);

            return _mapper.Map<CategoryDto>(updatedCategory);
        }
        catch (Exception ex) when (
            !(ex is EntityNotFoundException) &&
            !(ex is DuplicateEntityException) &&
            !(ex is ConcurrencyException) &&
            !(ex is InvalidOperationException) &&
            !(ex is EntityDeletedException))
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingCategory =
                await _unitOfWork.Categories.GetByIdWithDetailsAsync(id, true, true, cancellationToken);

            if (existingCategory == null) throw new EntityNotFoundException($"Category with ID {id} not found");

            if (existingCategory.IsDeleted) return true;

            var canDelete = await _unitOfWork.Categories.CanDeleteAsync(id, cancellationToken);
            if (!canDelete)
                throw new InvalidOperationException("This category cannot be deleted because it contains subcategories or documents. Please first delete these contents or move them to other categories.");

            existingCategory.IsDeleted = true;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            return await _unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await _unitOfWork.Categories.UpdateAsync(existingCategory, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex) when (
            !(ex is EntityNotFoundException) &&
            !(ex is ConcurrencyException) &&
            !(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            throw;
        }
    }

    /// <summary>
    /// Checks if setting newParentId as parent of categoryId would create a circular reference
    /// </summary>
    private async Task<bool> WouldCreateCircularReferenceAsync(Guid categoryId, Guid newParentId, CancellationToken cancellationToken)
    {
        try
        {
            var visited = new HashSet<Guid>();
            var currentId = newParentId;

            // Traverse up the hierarchy from newParentId
            while (currentId != Guid.Empty)
            {
                // If we encounter the categoryId we're trying to move, it would create a cycle
                if (currentId == categoryId)
                    return true;

                // Prevent infinite loop in case of existing circular references
                if (visited.Contains(currentId))
                    return true;

                visited.Add(currentId);

                // Get parent of current category
                var category = await _unitOfWork.Categories.GetByIdAsync(currentId, cancellationToken);
                if (category?.ParentCategoryId == null)
                    break;

                currentId = category.ParentCategoryId.Value;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking circular reference for category {CategoryId} with new parent {NewParentId}", 
                categoryId, newParentId);
            // In case of error, be conservative and assume it would create a circular reference
            return true;
        }
    }
}
