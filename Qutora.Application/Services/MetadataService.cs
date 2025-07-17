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
/// Metadata service implementation
/// </summary>
public class MetadataService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<MetadataService> logger,
    ICurrentUserService currentUserService)
    : IMetadataService
{
    /// <inheritdoc/>
    public async Task<MetadataDto?> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var metadata = await unitOfWork.Metadata.GetByDocumentIdAsync(documentId, cancellationToken);
        return metadata != null ? MapToDto(metadata) : null;
    }

    /// <inheritdoc/>
    public async Task<MetadataDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var metadata = await unitOfWork.Metadata.GetByIdWithDocumentDetailsAsync(id, cancellationToken);
        return metadata != null ? MapToDto(metadata) : null;
    }

    /// <inheritdoc/>
    public async Task<MetadataDto> CreateAsync(Guid documentId, CreateUpdateMetadataDto createMetadataDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if metadata already exists for this document
            var existingMetadata = await unitOfWork.Metadata.GetByDocumentIdAsync(documentId, cancellationToken);
            if (existingMetadata != null)
            {
                throw new InvalidOperationException($"Metadata already exists for document with ID {documentId}. Use update operation instead.");
            }

            Guid? metadataSchemaId = null;
            
            if (!string.IsNullOrEmpty(createMetadataDto.SchemaName))
            {
                var schema =
                    await unitOfWork.MetadataSchemas.GetByNameAsync(createMetadataDto.SchemaName, cancellationToken);

                if (schema == null)
                    throw new KeyNotFoundException(
                        $"Metadata schema with name '{createMetadataDto.SchemaName}' not found.");

                if (!schema.IsActive)
                    throw new InvalidOperationException(
                        $"Metadata schema with name '{createMetadataDto.SchemaName}' is not active.");

                metadataSchemaId = schema.Id;

                if (createMetadataDto.Values?.Count > 0)
                {
                    var validationErrors = await ValidateMetadataAsync(createMetadataDto.SchemaName, createMetadataDto.Values, cancellationToken);
                    if (validationErrors.Count > 0)
                    {
                        var errorMessage = string.Join("; ", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                        throw new ArgumentException($"Metadata validation failed: {errorMessage}");
                    }
                }
            }

            var metadata = new Metadata
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                SchemaName = createMetadataDto.SchemaName ?? string.Empty,
                SchemaVersion = "1.0",
                MetadataSchemaId = metadataSchemaId,
                MetadataJson = SerializeMetadata(createMetadataDto.Values, createMetadataDto.Tags),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserService.UserId ?? string.Empty
            };

            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.Metadata.AddAsync(metadata, cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);

                return MapToDto(metadata);
            }, cancellationToken);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while creating metadata: {DocumentId}", documentId);
            throw;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException && ex is not InvalidOperationException && ex is not ArgumentException)
        {
            logger.LogError(ex, "Error occurred while creating metadata: {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<MetadataDto> UpdateAsync(Guid documentId, CreateUpdateMetadataDto updateMetadataDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await unitOfWork.Metadata.GetByDocumentIdAsync(documentId, cancellationToken);

            if (metadata == null)
                throw new KeyNotFoundException($"Metadata for document with ID {documentId} not found.");

            // Security: Prevent schema changes during metadata update
            if (!string.IsNullOrEmpty(updateMetadataDto.SchemaName) &&
                metadata.SchemaName != updateMetadataDto.SchemaName)
            {
                throw new InvalidOperationException(
                    "Schema cannot be changed during metadata update. This is a security restriction to maintain data integrity.");
            }

            if (!string.IsNullOrEmpty(updateMetadataDto.SchemaName) && updateMetadataDto.Values?.Count > 0)
            {
                var validationErrors = await ValidateMetadataAsync(updateMetadataDto.SchemaName, updateMetadataDto.Values, cancellationToken);
                if (validationErrors.Count > 0)
                {
                    var errorMessage = string.Join("; ", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                    throw new ArgumentException($"Metadata validation failed: {errorMessage}");
                }
            }

            metadata.MetadataJson = SerializeMetadata(updateMetadataDto.Values, updateMetadataDto.Tags);
            metadata.UpdatedAt = DateTime.UtcNow;
            metadata.UpdatedBy = currentUserService.UserId;

            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.Metadata.UpdateAsync(metadata, cancellationToken);
                return MapToDto(metadata);
            }, cancellationToken);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while updating metadata: {DocumentId}", documentId);
            throw;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException && ex is not ArgumentException)
        {
            logger.LogError(ex, "Error occurred while updating metadata: {DocumentId}", documentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await unitOfWork.Metadata.GetByIdAsync(id, cancellationToken);

            if (metadata == null) return false;

            metadata.IsDeleted = true;
            metadata.UpdatedAt = DateTime.UtcNow;
            metadata.UpdatedBy = currentUserService.UserId;

            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.Metadata.UpdateAsync(metadata, cancellationToken);
                return true;
            }, cancellationToken);
        }
        catch (ConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency error occurred while deleting metadata: {MetadataId}", id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting metadata: {MetadataId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PagedDto<MetadataDto>> GetByTagsAsync(string[] tags, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var metadataList = await unitOfWork.Metadata.GetByTagsAsync(tags, page, pageSize, cancellationToken);
        var totalCount = await unitOfWork.Metadata.GetByTagsCountAsync(tags, cancellationToken);

        return new PagedDto<MetadataDto>
        {
            Items = metadataList.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <inheritdoc/>
    public async Task<PagedDto<MetadataDto>> SearchAsync(Dictionary<string, object> searchCriteria, int page = 1,
        int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var metadataList = await unitOfWork.Metadata.SearchAsync(searchCriteria, page, pageSize, cancellationToken);
        var totalCount = await unitOfWork.Metadata.SearchCountAsync(searchCriteria, cancellationToken);

        return new PagedDto<MetadataDto>
        {
            Items = metadataList.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, string>> ValidateMetadataAsync(string schemaName,
        Dictionary<string, object> metadata, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string>();

        var schema = await unitOfWork.MetadataSchemas.GetByNameAsync(schemaName, cancellationToken);

        if (schema == null) throw new KeyNotFoundException($"Metadata schema with name '{schemaName}' not found.");

        if (!schema.IsActive)
        {
            errors.Add("schema", $"Schema '{schemaName}' is not active.");
            return errors;
        }

        try
        {
            var fields = schema.Fields;

            foreach (var field in fields)
            {
                if (field.IsRequired && !metadata.ContainsKey(field.Name))
                {
                    errors.Add(field.Name, $"Field '{field.Name}' is required.");
                    continue;
                }

                if (!metadata.ContainsKey(field.Name)) continue;

                var value = metadata[field.Name];

                if (!IsValidFieldType(value, (int)field.Type))
                {
                    errors.Add(field.Name, $"Field '{field.Name}' has invalid type. Expected {field.Type}.");
                    continue;
                }

                switch (field.Type)
                {
                    case MetadataType.Text:
                    {
                        var textValue = GetStringValue(value);

                        if (field.MinLength.HasValue && textValue.Length < field.MinLength.Value)
                            errors.Add(field.Name,
                                $"Field '{field.Name}' must be at least {field.MinLength.Value} characters.");

                        if (field.MaxLength.HasValue && textValue.Length > field.MaxLength.Value)
                            errors.Add(field.Name,
                                $"Field '{field.Name}' must be at most {field.MaxLength.Value} characters.");

                        if (!string.IsNullOrEmpty(field.ValidationRegex))
                            try
                            {
                                var regex = new System.Text.RegularExpressions.Regex(field.ValidationRegex);

                                if (!regex.IsMatch(textValue))
                                    errors.Add(field.Name, $"Field '{field.Name}' does not match validation pattern.");
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Invalid regex pattern for field '{FieldName}': {Pattern}", field.Name,
                                    field.ValidationRegex);
                            }

                        break;
                    }
                    case MetadataType.Number:
                    {
                        decimal numValue;
                        if (!decimal.TryParse(GetStringValue(value), out numValue))
                        {
                            errors.Add(field.Name, $"Field '{field.Name}' has invalid number format.");
                            continue;
                        }

                        if (field.MinValue.HasValue && numValue < field.MinValue.Value)
                            errors.Add(field.Name, $"Field '{field.Name}' must be at least {field.MinValue.Value}.");

                        if (field.MaxValue.HasValue && numValue > field.MaxValue.Value)
                            errors.Add(field.Name, $"Field '{field.Name}' must be at most {field.MaxValue.Value}.");
                        break;
                    }
                    case MetadataType.Select:
                    {
                        var selectValue = GetStringValue(value);

                        if (!string.IsNullOrEmpty(selectValue) && field.OptionItems.All(o => o.Value != selectValue))
                        {
                            var validOptions = string.Join(", ", field.OptionItems.Select(o => o.Value));
                            errors.Add(field.Name,
                                $"Field '{field.Name}' has an invalid option. Valid options are: {validOptions}");
                        }

                        break;
                    }
                    case MetadataType.MultiSelect:
                    {
                        var selectedValues = GetStringArrayValue(value);

                        if (selectedValues.Length > 0)
                        {
                            var validOptionValues = field.OptionItems.Select(o => o.Value).ToList();

                            foreach (var selectedValue in selectedValues)
                                if (!validOptionValues.Contains(selectedValue))
                                {
                                    var validOptions = string.Join(", ", validOptionValues);
                                    errors.Add(field.Name,
                                        $"Field '{field.Name}' has an invalid option '{selectedValue}'. Valid options are: {validOptions}");
                                    break;
                                }
                        }

                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating metadata for schema '{SchemaName}'", schemaName);
            errors.Add("_error", "An error occurred while validating metadata: " + ex.Message);
        }

        return errors;
    }

    /// <summary>
    /// Converts metadata object to DTO
    /// </summary>
    private MetadataDto MapToDto(Metadata metadata)
    {
        var dto = mapper.Map<MetadataDto>(metadata);

        var metadataObject = DeserializeMetadata(metadata.MetadataJson);

        dto.Values = metadataObject.Values ?? new Dictionary<string, object>();
        dto.Tags = metadataObject.Tags ?? [];

        if (metadata.Document != null) dto.DocumentName = metadata.Document.Name;

        return dto;
    }

    /// <summary>
    /// Converts metadata values and tags to JSON format
    /// </summary>
    private string SerializeMetadata(Dictionary<string, object> values, string[]? tags = null)
    {
        var metadataObject = new
        {
            Values = values,
            Tags = tags ?? []
        };

        return JsonSerializer.Serialize(metadataObject);
    }

    /// <summary>
    /// Converts JSON format metadata
    /// </summary>
    private (Dictionary<string, object>? Values, string[]? Tags) DeserializeMetadata(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Dictionary<string, object>? values = null;
            if (root.TryGetProperty("Values", out var valuesElement))
                values = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    valuesElement.GetRawText(), options);

            string[]? tags = null;
            if (root.TryGetProperty("Tags", out var tagsElement))
                tags = JsonSerializer.Deserialize<string[]>(tagsElement.GetRawText(), options);

            return (values ?? new Dictionary<string, object>(), tags ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deserializing metadata JSON: {Json}", json);
            return (new Dictionary<string, object>(), []);
        }
    }

    /// <summary>
    /// Checks whether the value is suitable for the specified type
    /// </summary>
    private bool IsValidFieldType(object value, int typeId)
    {
        if (value is JsonElement jsonElement) return IsValidJsonElementType(jsonElement, typeId);

        switch (typeId)
        {
            case 0: // Text
                return value is string;

            case 1: // Number
                if (value is int || value is long || value is float || value is double || value is decimal)
                    return true;

                if (value is string numStr)
                    return decimal.TryParse(numStr, out _);

                return false;

            case 2: // DateTime
                if (value is string dateStr) return DateTime.TryParse(dateStr, out _);
                return value is DateTime;

            case 3: // Boolean
                if (value is bool)
                    return true;

                if (value is string boolStr)
                    return bool.TryParse(boolStr, out _);

                return false;

            case 4: // Select
                return value is string;

            case 5: // MultiSelect
                if (value is string)
                    return true;

                return value is Array || value is IEnumerable<string>;

            case 6: // Reference
                return value is string;

            default:
                return true;
        }
    }

    /// <summary>
    /// Checks whether JsonElement is suitable for the specified type
    /// </summary>
    private bool IsValidJsonElementType(JsonElement element, int typeId)
    {
        switch (typeId)
        {
            case 0: // Text
                return element.ValueKind == JsonValueKind.String;

            case 1: // Number
                if (element.ValueKind == JsonValueKind.Number)
                    return true;

                if (element.ValueKind == JsonValueKind.String)
                    return decimal.TryParse(element.GetString(), out _);

                return false;

            case 2: // DateTime
                if (element.ValueKind == JsonValueKind.String)
                    return DateTime.TryParse(element.GetString(), out _);

                return false;

            case 3: // Boolean
                if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    return true;

                if (element.ValueKind == JsonValueKind.String)
                    return bool.TryParse(element.GetString(), out _);

                return false;

            case 4: // Select
                return element.ValueKind == JsonValueKind.String;

            case 5: // MultiSelect
                if (element.ValueKind == JsonValueKind.String)
                    return true;

                if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                        if (item.ValueKind != JsonValueKind.String)
                            return false;
                    return true;
                }

                return false;

            case 6: // Reference
                return element.ValueKind == JsonValueKind.String;

            default:
                return true;
        }
    }

    /// <summary>
    /// Extracts string value from a value (with JsonElement support)
    /// </summary>
    private string GetStringValue(object value)
    {
        switch (value)
        {
            case string strValue:
                return strValue;
            case JsonElement { ValueKind: JsonValueKind.String } jsonElement:
                return jsonElement.GetString() ?? string.Empty;
            default:
                return value?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Extracts string array from a value (with JsonElement support)
    /// </summary>
    private string[] GetStringArrayValue(object value)
    {
        if (value is string[] strArray) return strArray;

        if (value is string singleValue) return [singleValue];

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.String) return [jsonElement.GetString() ?? string.Empty];

            if (jsonElement.ValueKind == JsonValueKind.Array)
                try
                {
                    var list = new List<string>();
                    foreach (var element in jsonElement.EnumerateArray())
                        if (element.ValueKind == JsonValueKind.String)
                            list.Add(element.GetString() ?? string.Empty);
                        else
                            list.Add(element.ToString());

                    return list.ToArray();
                }
                catch
                {
                    return [];
                }
        }

        if (value is IEnumerable<string> enumerable) return enumerable.ToArray();

        if (value is Array array)
        {
            var list = new List<string>();
            foreach (var item in array) list.Add(item?.ToString() ?? string.Empty);
            return list.ToArray();
        }

        return [];
    }
}
