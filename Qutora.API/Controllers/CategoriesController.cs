using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapsterMapper;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Exceptions;
using Qutora.Infrastructure.Interfaces;
using Qutora.Shared.DTOs;

namespace Qutora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController(
    ICategoryService categoryService,
    ILogger<CategoriesController> logger,
    IMapper mapper)
    : ControllerBase
{
    private readonly ICategoryService _categoryService =
        categoryService ?? throw new ArgumentNullException(nameof(categoryService));

    private readonly ILogger<CategoriesController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    [HttpGet]
    [Authorize(Policy = "Category.Read")]
    public async Task<IActionResult> GetAllCategories([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string searchTerm = "")
    {
        try
        {
            // A new method needs to be added to the service interface for pagination
            // For now, let's get all categories and do client-side pagination
            var categoryDtos = await _categoryService.GetAllAsync();
            
            // Arama filtresi
            if (!string.IsNullOrEmpty(searchTerm))
            {
                categoryDtos = categoryDtos.Where(c => 
                    c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(c.Description) && c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                );
            }
            
            // Sayfalama
            var totalCount = categoryDtos.Count();
            var pagedCategories = categoryDtos
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var result = new PagedDto<CategoryDto>
            {
                Items = pagedCategories,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }
    
    [HttpGet("all")]
    [Authorize(Policy = "Category.Read")]
    public async Task<IActionResult> GetAllCategoriesSimple()
    {
        try
        {
            var categoryDtos = await _categoryService.GetAllAsync();
            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all categories");
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }

    [HttpGet("root")]
    [Authorize(Policy = "Category.Read")]
    public async Task<IActionResult> GetRootCategories()
    {
        try
        {
            var categoryDtos = await _categoryService.GetRootCategoriesAsync();
            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving root categories");
            return StatusCode(500, "An error occurred while retrieving root categories");
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Category.Read")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        try
        {
            var categoryDto = await _categoryService.GetByIdAsync(id);

            if (categoryDto == null) return NotFound();

            return Ok(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", id);
            return StatusCode(500, "An error occurred while retrieving the category");
        }
    }

    [HttpGet("{id}/subcategories")]
    [Authorize(Policy = "Category.Read")]
    public async Task<IActionResult> GetSubCategories(Guid id)
    {
        try
        {
            var categoryDtos = await _categoryService.GetSubCategoriesAsync(id);
            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subcategories for category ID {CategoryId}", id);
            return StatusCode(500, "An error occurred while retrieving subcategories");
        }
    }

    [HttpPost]
    [Authorize(Policy = "Category.Create")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto createCategoryDto)
    {
        try
        {
            if (createCategoryDto == null) return BadRequest("Category data is required");

            var categoryDto = await _categoryService.CreateAsync(createCategoryDto);

            return CreatedAtAction(nameof(GetCategory), new { id = categoryDto.Id }, categoryDto);
        }
        catch (DuplicateEntityException ex)
        {
            _logger.LogWarning(ex, "Duplicate category name: {CategoryName}", createCategoryDto.Name);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, "An error occurred while creating the category");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Category.Update")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto updateCategoryDto)
    {
        try
        {
            if (updateCategoryDto == null) return BadRequest("Category data is required");

            if (id != updateCategoryDto.Id) return BadRequest("ID mismatch");

            var categoryDto = await _categoryService.UpdateAsync(id, updateCategoryDto);

            return Ok(categoryDto);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Category not found for update: {CategoryId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (EntityDeletedException ex)
        {
            _logger.LogWarning(ex, "Attempt to update deleted category: {CategoryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (DuplicateEntityException ex)
        {
            _logger.LogWarning(ex, "Duplicate category name during update: {CategoryName}", updateCategoryDto.Name);
            return BadRequest(new { message = ex.Message });
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating category: {CategoryId}", id);
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during category update: {CategoryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, "An error occurred while updating the category");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Category.Delete")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try
        {
            var result = await _categoryService.DeleteAsync(id);

            if (!result) return NotFound();

            return NoContent();
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Category not found for delete: {CategoryId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during category delete: {CategoryId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict deleting category: {CategoryId}", id);
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, "An error occurred while deleting the category");
        }
    }
}
