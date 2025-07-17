using System.Text.Json;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;
using Qutora.Shared.Enums;
using Qutora.Shared.Exceptions;

namespace Qutora.Application.Services;

/// <summary>
/// Metadata schema service implementation
/// </summary>
public class MetadataSchemaService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<MetadataSchemaService> logger,
    ICurrentUserService currentUserService)
    : IMetadataSchemaService
{
    /// <inheritdoc/> 
    public async Task<PagedDto<MetadataSchemaDto>> GetAllAsync(int page = 1, int pageSize = 10, string query = "",
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var schemas = await unitOfWork.MetadataSchemas.GetAllPagedAsync(page, pageSize, query, cancellationToken);
        var totalCount = await unitOfWork.MetadataSchemas.GetTotalCountAsync(query, cancellationToken);

        var pagedDto = new PagedDto<MetadataSchemaDto>
        {
            Items = mapper.Map<List<MetadataSchemaDto>>(schemas),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return pagedDto;
    }

    /// <inheritdoc/>
    public async Task<MetadataSchemaDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var schema = await unitOfWork.MetadataSchemas.GetByIdAsync(id, cancellationToken);
        return schema != null ? mapper.Map<MetadataSchemaDto>(schema) : null;
    }

    /// <inheritdoc/>
    public async Task<MetadataSchemaDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var schema = await unitOfWork.MetadataSchemas.GetByNameAsync(name, cancellationToken);
        return schema != null ? mapper.Map<MetadataSchemaDto>(schema) : null;
    }

    /// <inheritdoc/>
    public async Task<MetadataSchemaDto> CreateAsync(CreateUpdateMetadataSchemaDto createSchemaDto,
        CancellationToken cancellationToken = default)
    {
        var existingSchema = await unitOfWork.MetadataSchemas.GetByNameAsync(createSchemaDto.Name, cancellationToken);

        if (existingSchema != null)
            throw new InvalidOperationException($"Schema with name '{createSchemaDto.Name}' already exists.");

        ValidateSchemaFields(createSchemaDto.Fields);

        var schema = new MetadataSchema
        {
            Id = Guid.NewGuid(),
            Name = createSchemaDto.Name,
            Description = createSchemaDto.Description ?? string.Empty,
            Version = "1.0",
            IsActive = true,
            FileTypes = string.Join(",", createSchemaDto.FileTypes ?? []),
            CategoryId = createSchemaDto.CategoryId,

            SchemaDefinitionJson = JsonSerializer.Serialize(createSchemaDto.Fields.Select(f =>
                new MetadataSchemaField
                {
                    Name = f.Name,
                    DisplayName = f.DisplayName,
                    Description = f.Description ?? string.Empty,
                    Type = f.Type,
                    IsRequired = f.IsRequired,
                    DefaultValue = f.DefaultValue?.ToString() ?? string.Empty,
                    MinValue = f.MinValue,
                    MaxValue = f.MaxValue,
                    MinLength = f.MinLength,
                    MaxLength = f.MaxLength,
                    ValidationRegex = f.ValidationRegex ?? string.Empty,
                    Order = f.Order,
                    OptionItems = f.OptionItems?.Select(o => new MetadataSchemaFieldOption
                    {
                        Label = o.Label,
                        Value = o.Value,
                        IsDefault = o.IsDefault,
                        Order = o.Order
                    }).ToList() ?? []
                })),

            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserService.UserId ?? string.Empty
        };

        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            await unitOfWork.MetadataSchemas.AddAsync(schema, cancellationToken);
            return mapper.Map<MetadataSchemaDto>(schema);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MetadataSchemaDto> UpdateAsync(Guid id, CreateUpdateMetadataSchemaDto updateSchemaDto,
        CancellationToken cancellationToken = default)
    {
        var schema = await unitOfWork.MetadataSchemas.GetByIdAsync(id, cancellationToken);

        if (schema == null) throw new KeyNotFoundException($"Schema with ID {id} not found.");

        if (schema.Name != updateSchemaDto.Name)
        {
            var existingSchema =
                await unitOfWork.MetadataSchemas.GetByNameAsync(updateSchemaDto.Name, cancellationToken);

            if (existingSchema != null && existingSchema.Id != id)
                throw new InvalidOperationException($"Schema with name '{updateSchemaDto.Name}' already exists.");
        }

        ValidateSchemaFields(updateSchemaDto.Fields);

        schema.Name = updateSchemaDto.Name;
        schema.Description = updateSchemaDto.Description ?? string.Empty;
        schema.FileTypes = string.Join(",", updateSchemaDto.FileTypes ?? []);
        schema.CategoryId = updateSchemaDto.CategoryId;

        var schemaFieldsJson = JsonSerializer.Serialize(updateSchemaDto.Fields.Select(f => new MetadataSchemaField
        {
            Name = f.Name,
            DisplayName = f.DisplayName,
            Description = f.Description ?? string.Empty,
            Type = f.Type,
            IsRequired = f.IsRequired,
            DefaultValue = f.DefaultValue?.ToString() ?? string.Empty,
            MinValue = f.MinValue,
            MaxValue = f.MaxValue,
            MinLength = f.MinLength,
            MaxLength = f.MaxLength,
            ValidationRegex = f.ValidationRegex ?? string.Empty,
            Order = f.Order,
            OptionItems = f.OptionItems?.Select(o => new MetadataSchemaFieldOption
            {
                Label = o.Label,
                Value = o.Value,
                IsDefault = o.IsDefault,
                Order = o.Order
            }).ToList() ?? []
        }));

        schema.SchemaDefinitionJson = schemaFieldsJson;
        schema.UpdatedAt = DateTime.UtcNow;
        schema.UpdatedBy = currentUserService.UserId ?? string.Empty;

        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            await unitOfWork.MetadataSchemas.UpdateAsync(schema, cancellationToken);
            return mapper.Map<MetadataSchemaDto>(schema);
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var schema = await unitOfWork.MetadataSchemas.GetByIdAsync(id, cancellationToken);
            if (schema == null) return false;

            var metadataRecords =
                await unitOfWork.Metadata.FindAsync(m => m.SchemaName == schema.Name && !m.IsDeleted,
                    cancellationToken);
            if (metadataRecords.Any())
            {
                schema.IsActive = false;
                schema.UpdatedAt = DateTime.UtcNow;
                schema.UpdatedBy = currentUserService.UserId ?? string.Empty;
            }
            else
            {
                schema.IsDeleted = true;
                schema.UpdatedAt = DateTime.UtcNow;
                schema.UpdatedBy = currentUserService.UserId ?? string.Empty;
            }

            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.MetadataSchemas.UpdateAsync(schema, cancellationToken);
                return true;
            }, cancellationToken);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while deleting schema: {SchemaId}", id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting schema: {SchemaId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MetadataSchemaDto>> GetByFileTypeAsync(string fileType,
        CancellationToken cancellationToken = default)
    {
        var schemas = await unitOfWork.MetadataSchemas.GetByFileTypeAsync(fileType, cancellationToken);
        return mapper.Map<IEnumerable<MetadataSchemaDto>>(schemas);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MetadataSchemaDto>> GetByCategoryIdAsync(Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var schemas = await unitOfWork.MetadataSchemas.GetByCategoryIdAsync(categoryId, cancellationToken);
        return mapper.Map<IEnumerable<MetadataSchemaDto>>(schemas);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<MetadataSchemaDto>> GetAllSchemasAsync(CancellationToken cancellationToken = default)
    {
        var schemas = await unitOfWork.MetadataSchemas.GetActiveAsync(cancellationToken);

        var simplifiedDtos = schemas.Select(s => new MetadataSchemaDto
        {
            Id = s.Id,
            Name = s.Name,
            CategoryId = s.CategoryId
        });

        return simplifiedDtos;
    }

    /// <summary>
    /// Validates schema field definitions
    /// </summary>
    private void ValidateSchemaFields(List<CreateUpdateMetadataSchemaFieldDto> fields)
    {
        if (fields == null || !fields.Any()) throw new ArgumentException("Schema must have at least one field.");

        var fieldNames = fields.Select(f => f.Name.ToLower()).ToList();
        if (fieldNames.Count != fieldNames.Distinct().Count())
            throw new ArgumentException("Field names must be unique.");

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name)) throw new ArgumentException("Field name cannot be empty.");

            if (string.IsNullOrWhiteSpace(field.DisplayName))
                field.DisplayName = field.Name;

            if (field.Name.Contains(" ") || field.Name.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                throw new ArgumentException(
                    $"Field name '{field.Name}' contains invalid characters. Use only letters, numbers and underscores.");

            if ((int)field.Type < 0 || (int)field.Type > 6)
                throw new ArgumentException(
                    $"Invalid field type for field '{field.Name}'. Type must be between 0 and 6.");

            if (field.Type == MetadataType.Select || field.Type == MetadataType.MultiSelect)
            {
                if (field.OptionItems == null || !field.OptionItems.Any())
                    throw new ArgumentException($"Field '{field.Name}' of type Select/MultiSelect must have at least one option defined.");

                foreach (var option in field.OptionItems)
                {
                    if (string.IsNullOrWhiteSpace(option.Label))
                        throw new ArgumentException($"Option in field '{field.Name}' has empty label.");

                    if (string.IsNullOrWhiteSpace(option.Value))
                        option.Value = option.Label;
                }

                if (field.Type == MetadataType.Select && field.OptionItems.Count(o => o.IsDefault) > 1)
                    throw new ArgumentException(
                        $"Field '{field.Name}' of type Select can have at most one default option.");
            }
        }
    }
}
