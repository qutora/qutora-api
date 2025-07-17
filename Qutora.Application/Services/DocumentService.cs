using System.Text.Json;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Domain.Entities;
using Qutora.Shared.DTOs;

namespace Qutora.Application.Services;

/// <summary>
/// Service implementation for document operations.
/// All file operations are first saved to the database, then storage operations are performed.
/// </summary>
public class DocumentService(
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorageService,
    IMetadataService metadataService,
    IMetadataSchemaService metadataSchemaService,
    ILogger<DocumentService> logger,
    ICurrentUserService currentUserService,
    IDocumentVersionService documentVersionService,
    IDocumentShareService documentShareService,
    IMapper mapper)
    : IDocumentService
{

    public async Task<DocumentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await unitOfWork.Documents.GetByIdWithDetailsAsync(id, cancellationToken);
        return document != null ? mapper.Map<DocumentDto>(document) : null;
    }

    public async Task<PagedDto<DocumentDto>> GetDocumentsAsync(string? query = null, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var documents = await unitOfWork.Documents.GetDocumentsAsync(query, page, pageSize, cancellationToken);
        var totalCount = await unitOfWork.Documents.GetDocumentsCountAsync(query, cancellationToken);
        
        return new PagedDto<DocumentDto>
        {
            Items = mapper.Map<List<DocumentDto>>(documents),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedDto<DocumentDto>> GetByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var documents = await unitOfWork.Documents.GetByCategoryAsync(categoryId, page, pageSize, cancellationToken);
        var totalCount = await unitOfWork.Documents.GetByCategoryCountAsync(categoryId, cancellationToken);
        
        return new PagedDto<DocumentDto>
        {
            Items = mapper.Map<List<DocumentDto>>(documents),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Gets all documents
    /// </summary>
    public async Task<IEnumerable<DocumentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = await unitOfWork.Documents.GetAllAsync(cancellationToken);
            return mapper.Map<IEnumerable<DocumentDto>>(documents);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while retrieving all documents");
            return [];
        }
    }

    /// <summary>
    /// Gets documents in a specific bucket
    /// </summary>
    public async Task<PagedDto<DocumentDto>> GetDocumentsByBucketAsync(Guid bucketId, int page = 1,
        int pageSize = 10, string? query = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId, cancellationToken);
            if (bucket == null)
            {
                logger.LogWarning("Bucket not found: {BucketId}", bucketId);
                return new PagedDto<DocumentDto>
                {
                    Items = new List<DocumentDto>(),
                    TotalCount = 0,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }

            var documents = await unitOfWork.Documents.GetByBucketIdAsync(bucketId, page, pageSize, query, cancellationToken);
            var totalCount = await unitOfWork.Documents.GetByBucketIdCountAsync(bucketId, query, cancellationToken);
            
            return new PagedDto<DocumentDto>
            {
                Items = mapper.Map<List<DocumentDto>>(documents),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving bucket documents: {BucketId}, Page {Page}, Size {PageSize}",
                bucketId, page, pageSize);
            return new PagedDto<DocumentDto>
            {
                Items = new List<DocumentDto>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = 0
            };
        }
    }

    public async Task<PagedDto<DocumentDto>> GetDocumentsByProviderAsync(Guid providerId, int page = 1,
        int pageSize = 10, string? query = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = await unitOfWork.StorageProviders.GetByIdAsync(providerId, cancellationToken);
            if (provider == null)
            {
                logger.LogWarning("Provider not found: {ProviderId}", providerId);
                return new PagedDto<DocumentDto>
                {
                    Items = new List<DocumentDto>(),
                    TotalCount = 0,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }

            var documents = await unitOfWork.Documents.GetByProviderIdAsync(providerId, page, pageSize, query, cancellationToken);
            var totalCount = await unitOfWork.Documents.GetByProviderIdCountAsync(providerId, query, cancellationToken);
            
            return new PagedDto<DocumentDto>
            {
                Items = mapper.Map<List<DocumentDto>>(documents),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving provider documents: {ProviderId}, Page {Page}, Size {PageSize}",
                providerId, page, pageSize);
            return new PagedDto<DocumentDto>
            {
                Items = new List<DocumentDto>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = 0
            };
        }
    }

    /// <summary>
    /// Gets documents from multiple buckets (for user accessible documents)
    /// </summary>
    public async Task<PagedDto<DocumentDto>> GetDocumentsByBucketIdsAsync(IEnumerable<Guid> bucketIds, int page = 1, 
        int pageSize = 10, string? query = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var bucketIdsList = bucketIds.ToList();
            
            if (!bucketIdsList.Any())
            {
                logger.LogInformation("No bucket IDs provided for user accessible documents");
                return new PagedDto<DocumentDto>
                {
                    Items = new List<DocumentDto>(),
                    TotalCount = 0,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalPages = 0
                };
            }

            var documents = await unitOfWork.Documents.GetByBucketIdsAsync(bucketIdsList, page, pageSize, query, cancellationToken);
            var totalCount = await unitOfWork.Documents.GetByBucketIdsCountAsync(bucketIdsList, query, cancellationToken);
            
            return new PagedDto<DocumentDto>
            {
                Items = mapper.Map<List<DocumentDto>>(documents),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "User accessible documents retrieval error - Page {Page}, Size {PageSize}",
                page, pageSize);
            return new PagedDto<DocumentDto>
            {
                Items = new List<DocumentDto>(),
                TotalCount = 0,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = 0
            };
        }
    }

    /// <summary>
    /// Creates document with DTO
    /// </summary>
    public async Task<DocumentDto> CreateAsync(DocumentCreateDto documentDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (documentDto.File == null || documentDto.File.Length == 0)
                throw new ArgumentException("File not found or empty");

            var schema = documentDto.MetadataSchemaId.HasValue
                ? await metadataSchemaService.GetByIdAsync(documentDto.MetadataSchemaId.Value, cancellationToken)
                : null;

            var (createdDocumentDto, shareDto) = await CreateDocumentWithFileAsync(
                documentDto.File,
                documentDto.Name,
                documentDto.CategoryId,
                documentDto.StorageProviderId,
                documentDto.BucketId,
                documentDto.MetadataValues,
                schema?.Name,
                null,
                cancellationToken);

            return createdDocumentDto;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException &&
                                   ex is not KeyNotFoundException)
        {
            logger.LogError(ex, "Error creating document with DTO");
            throw;
        }
    }

    /// <summary>
    /// Updates document with ID and DTO
    /// </summary>
    public async Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto documentDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await unitOfWork.Documents.GetByIdAsync(id, cancellationToken);
            if (document == null) throw new KeyNotFoundException($"Document with ID '{id}' not found.");

            document.Name = documentDto.Name;
            document.CategoryId = documentDto.CategoryId;
            document.BucketId = documentDto.BucketId;
            document.UpdatedAt = DateTime.UtcNow;
            document.UpdatedBy = currentUserService.UserId;

            var updatedDocument = await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.Documents.UpdateAsync(document, cancellationToken);
                return document;
            }, cancellationToken);

            return mapper.Map<DocumentDto>(updatedDocument);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            logger.LogError(ex, "Error updating document: {DocumentId}", id);
            throw;
        }
    }

    /// <summary>
    /// Creates document with file and additional information (legacy signature)
    /// </summary>
    public async Task<(DocumentDto Document, DocumentShareDto? Share)> CreateDocumentWithFileAsync(
        IFormFile file,
        string name,
        Guid? categoryId = null,
        Guid? storageProviderId = null,
        string? metadataJson = null,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateDocumentWithFileAsync(file, name, categoryId, storageProviderId, null, metadataJson,
            schemaName, null, cancellationToken);
    }

    /// <summary>
    /// Creates document with file - refactored to prevent transaction timeouts
    /// </summary>
    public async Task<(DocumentDto Document, DocumentShareDto? Share)> CreateDocumentWithFileAsync(
        IFormFile file,
        string name,
        Guid? categoryId = null,
        Guid? storageProviderId = null,
        Guid? bucketId = null,
        string? metadataJson = null,
        string? schemaName = null,
        DocumentShareCreateDto? shareOptions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Validate inputs and permissions
            await ValidateDocumentCreationInputsAsync(file, name, storageProviderId, cancellationToken);

            // Step 2: Create document entity
            var document = CreateDocumentEntity(file, name, categoryId, bucketId);

            // Step 3: Process metadata if provided
            var metadataDto = PrepareMetadataDto(metadataJson, schemaName);

            // Step 4: Execute document creation in focused transaction
            var documentDto = await ExecuteDocumentCreationAsync(document, file, storageProviderId, metadataDto, cancellationToken);

            logger.LogInformation("Document created successfully. ID: {DocumentId}, Name: {DocumentName}",
                documentDto.Id, documentDto.Name);

            // Step 5: Handle share creation if requested (separate transaction to avoid timeout)
            var shareDto = await CreateShareIfRequestedAsync(documentDto, shareOptions, categoryId, bucketId, cancellationToken);

            return (documentDto, shareDto);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not UnauthorizedAccessException &&
                                   ex is not InvalidOperationException && ex is not KeyNotFoundException)
        {
            logger.LogError(ex, "Error creating document: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Validates document creation inputs and user permissions
    /// </summary>
    private async Task ValidateDocumentCreationInputsAsync(IFormFile file, string name, Guid? storageProviderId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("User authentication required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Document name cannot be empty.", nameof(name));

        if (file.Length == 0)
            throw new ArgumentException("File cannot be empty.", nameof(file));

        if (storageProviderId.HasValue)
        {
            var provider = await unitOfWork.StorageProviders.GetByIdAsync(storageProviderId.Value, cancellationToken);
            if (provider == null)
                throw new KeyNotFoundException($"Storage provider not found: {storageProviderId}");
            
            if (!provider.IsActive)
                throw new InvalidOperationException($"Storage provider is not active: {provider.Name}");
        }
    }

    /// <summary>
    /// Creates document entity from input parameters
    /// </summary>
    private Document CreateDocumentEntity(IFormFile file, string name, Guid? categoryId, Guid? bucketId)
    {
        var userId = currentUserService.UserId!; // Already validated

        return new Document
        {
            Id = Guid.NewGuid(),
            Name = name,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            CategoryId = categoryId,
            BucketId = bucketId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };
    }

    /// <summary>
    /// Prepares metadata DTO from JSON input
    /// </summary>
    private CreateUpdateMetadataDto? PrepareMetadataDto(string? metadataJson, string? schemaName)
    {
        if (string.IsNullOrEmpty(metadataJson))
            return null;

        var metadataValues = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
        if (metadataValues is not { Count: > 0 })
            return null;

        return new CreateUpdateMetadataDto
        {
            SchemaName = schemaName ?? "Default",
            Values = metadataValues
        };
    }

    /// <summary>
    /// Executes document creation in a focused transaction
    /// </summary>
    private async Task<DocumentDto> ExecuteDocumentCreationAsync(
        Document document, 
        IFormFile file, 
        Guid? storageProviderId, 
        CreateUpdateMetadataDto? metadataDto,
        CancellationToken cancellationToken)
    {
        await using var fileStream = file.OpenReadStream();

        return await unitOfWork.ExecuteTransactionalAsync(async () =>
        {
            var createdDocument = await CreateDocumentInternalAsync(document, fileStream, storageProviderId, file.ContentType, cancellationToken);

            if (metadataDto != null)
                await CreateMetadataInternalAsync(createdDocument.Id, metadataDto, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return mapper.Map<DocumentDto>(createdDocument);
        }, cancellationToken);
    }

    /// <summary>
    /// Creates share if requested, with proper validation
    /// </summary>
    private async Task<DocumentShareDto?> CreateShareIfRequestedAsync(
        DocumentDto documentDto, 
        DocumentShareCreateDto? shareOptions, 
        Guid? categoryId, 
        Guid? bucketId, 
        CancellationToken cancellationToken)
    {
        if (shareOptions == null)
            return null;

        try
        {
            // Validate DirectAccess permissions if needed
            if (shareOptions.IsDirectShare)
            {
                await ValidateDirectAccessPermissionsAsync(documentDto.Id, categoryId, bucketId, cancellationToken);
            }

            shareOptions.DocumentId = documentDto.Id;
            var shareDto = await documentShareService.CreateShareAsync(shareOptions, cancellationToken);
            
            logger.LogInformation(
                "Document share created. DocumentId: {DocumentId}, ShareCode: {ShareCode}, IsDirectShare: {IsDirectShare}",
                documentDto.Id, shareDto.ShareCode, shareOptions.IsDirectShare);
            
            return shareDto;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Document created successfully but share creation failed. DocumentId: {DocumentId}, IsDirectShare: {IsDirectShare}",
                documentDto.Id, shareOptions.IsDirectShare);
            // Don't rethrow - document creation was successful
            return null;
        }
    }

    /// <summary>
    /// Validates DirectAccess permissions for bucket and category
    /// </summary>
    private async Task ValidateDirectAccessPermissionsAsync(Guid documentId, Guid? categoryId, Guid? bucketId, CancellationToken cancellationToken)
    {
        bool bucketAllowsDirectAccess = false;
        bool categoryAllowsDirectAccess = false;
        
        if (bucketId.HasValue)
        {
            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId.Value, cancellationToken);
            bucketAllowsDirectAccess = bucket?.AllowDirectAccess == true;
        }

        if (categoryId.HasValue)
        {
            var category = await unitOfWork.Categories.GetByIdAsync(categoryId.Value, cancellationToken);
            categoryAllowsDirectAccess = category?.AllowDirectAccess == true;
        }

        if (!bucketAllowsDirectAccess || !categoryAllowsDirectAccess)
        {
            logger.LogWarning(
                "Direct share creation denied for document {DocumentId}. Bucket allows: {BucketAllows}, Category allows: {CategoryAllows}",
                documentId, bucketAllowsDirectAccess, categoryAllowsDirectAccess);
            throw new InvalidOperationException("Direct share not allowed. Both bucket and category must have AllowDirectAccess enabled.");
        }

        logger.LogInformation("Direct share validation passed for document {DocumentId}", documentId);
    }

    /// <summary>
    /// Creates metadata for document using CreateUpdateMetadataDto and validates against schema
    /// </summary>
    private async Task<Metadata> CreateMetadataInternalAsync(Guid documentId, CreateUpdateMetadataDto createMetadataDto,
        CancellationToken cancellationToken = default)
    {
        // Find schema ID
        Guid? metadataSchemaId = null;
        if (!string.IsNullOrEmpty(createMetadataDto.SchemaName))
        {
            var schema = await unitOfWork.MetadataSchemas.GetByNameAsync(createMetadataDto.SchemaName, cancellationToken);
            metadataSchemaId = schema?.Id;

            if (schema != null && createMetadataDto.Values?.Count > 0)
            {
                var validationErrors = await metadataService.ValidateMetadataAsync(createMetadataDto.SchemaName, createMetadataDto.Values, cancellationToken);
                if (validationErrors.Count > 0)
                {
                    var errorMessage = string.Join("; ", validationErrors.Select(e => $"{e.Key}: {e.Value}"));
                    throw new ArgumentException($"Metadata validation failed: {errorMessage}");
                }
            }
        }

        // Create metadata JSON in correct format
        var metadataObject = new
        {
            Values = createMetadataDto.Values ?? new Dictionary<string, object>(),
            Tags = createMetadataDto.Tags ?? new string[0]
        };
        var metadataJson = JsonSerializer.Serialize(metadataObject);

        var metadata = new Metadata
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            SchemaName = createMetadataDto.SchemaName ?? string.Empty,
            SchemaVersion = "1.0",
            MetadataJson = metadataJson,
            MetadataSchemaId = metadataSchemaId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserService.UserId ?? string.Empty
        };

        await unitOfWork.Metadata.AddAsync(metadata, cancellationToken);
        return metadata;
    }

    /// <summary>
    /// Creates document using specified provider ID. Uses default provider if ProviderId is null.
    /// </summary>
    private async Task<Document> CreateDocumentInternalAsync(Document document, Stream fileStream, Guid? providerId,
        string contentType = "application/octet-stream", CancellationToken cancellationToken = default)
    {
        document.Id = Guid.NewGuid();
        document.CreatedAt = DateTime.UtcNow;
        document.IsDeleted = false;

        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("User identity not found. Authentication required.");
        document.CreatedBy = userId;

        if (string.IsNullOrWhiteSpace(document.Name)) document.Name = document.FileName;

        if (!providerId.HasValue)
        {
            var defaultProvider = await unitOfWork.StorageProviders
                .FindSingleAsync(p => p.IsDefault && p.IsActive, cancellationToken);

            if (defaultProvider == null)
                throw new InvalidOperationException("Default storage provider not found or not active.");

            document.StorageProviderId = defaultProvider.Id;
        }
        else
        {
            // SECURITY CHECK: Verify specified provider exists and is active
            var specifiedProvider = await unitOfWork.StorageProviders.GetByIdAsync(providerId.Value, cancellationToken);
            if (specifiedProvider == null)
                throw new KeyNotFoundException($"Specified storage provider not found: {providerId.Value}");
            
            if (!specifiedProvider.IsActive)
                throw new InvalidOperationException($"Specified storage provider is not active: {specifiedProvider.Name}");
            
            document.StorageProviderId = providerId.Value;
        }

        await unitOfWork.Documents.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var hash = await fileStorageService.GetFileHashAsync(fileStream);
        document.Hash = hash;

        fileStream.Position = 0;

        var changeDescription = "Initial version";

        // Call DocumentVersionService without transaction - normally we would call DocumentVersionService's WithoutTransaction method
        // but for refactoring purposes, we're creating a simple version for now
        var versionResult = await CreateDocumentVersionInternalAsync(
            document.Id,
            fileStream,
            document.FileName,
            contentType,
            userId,
            changeDescription,
            cancellationToken);

        document.CurrentVersionId = versionResult.Id;
        document.StoragePath = versionResult.StoragePath;

        await unitOfWork.Documents.UpdateAsync(document, cancellationToken);

        return document;
    }

    /// <summary>
    /// Internal method that calls DocumentVersionService without using transaction
    /// </summary>
    private async Task<DocumentVersion> CreateDocumentVersionInternalAsync(
        Guid documentId,
        Stream stream,
        string fileName,
        string contentType,
        string userId,
        string changeDescription,
        CancellationToken cancellationToken = default)
    {
        var existingVersions = await unitOfWork.DocumentVersions.GetByDocumentIdAsync(documentId);
        var newVersionNumber = existingVersions.Any() ? existingVersions.Max(v => v.VersionNumber) + 1 : 1;

        var document = await unitOfWork.Documents.GetByIdAsync(documentId, cancellationToken);
        if (document == null) throw new KeyNotFoundException($"Document not found: {documentId}");

        var providerId = document.StorageProviderId.ToString();
        var bucketId = document.BucketId;

        string? bucketPath = null;
        if (bucketId.HasValue)
        {
            var bucket = await unitOfWork.StorageBuckets.GetByIdAsync(bucketId.Value, cancellationToken);
            bucketPath = bucket?.Path;
            logger.LogInformation(
                "Document upload with bucket - DocumentId: {DocumentId}, BucketId: {BucketId}, BucketPath: {BucketPath}",
                documentId, bucketId.Value, bucketPath);
        }
        else
        {
            // If no bucket is selected, use the provider's default bucket
            var defaultBucket = await unitOfWork.StorageBuckets.GetDefaultBucketForProviderAsync(document.StorageProviderId, cancellationToken);
            if (defaultBucket != null)
            {
                bucketPath = defaultBucket.Path;
                document.BucketId = defaultBucket.Id; // Also update the document entity
                logger.LogInformation(
                    "Using default bucket for document upload - DocumentId: {DocumentId}, DefaultBucketId: {BucketId}, BucketPath: {BucketPath}",
                    documentId, defaultBucket.Id, bucketPath);
            }
            else
            {
                logger.LogWarning(
                    "No default bucket found for provider - DocumentId: {DocumentId}, ProviderId: {ProviderId}",
                    documentId, document.StorageProviderId);
            }
        }

        var uploadResult = await fileStorageService.UploadFileAsync(
            providerId,
            stream,
            fileName,
            documentId.ToString(),
            contentType,
            bucketPath,
            cancellationToken);

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            VersionNumber = newVersionNumber,
            FileName = uploadResult.FileName,
            StoragePath = uploadResult.StoragePath,
            FileSize = uploadResult.FileSize,
            MimeType = uploadResult.ContentType,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            ChangeDescription = changeDescription,
            Hash = uploadResult.FileHash
        };

        await unitOfWork.DocumentVersions.AddAsync(version, cancellationToken);

        return version;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await unitOfWork.Documents.GetByIdAsync(id, cancellationToken);
            if (document == null || document.IsDeleted) return false;

            document.IsDeleted = true;
            document.DeletedAt = DateTime.UtcNow;

            return await unitOfWork.ExecuteTransactionalAsync(async () =>
            {
                await unitOfWork.Documents.UpdateAsync(document, cancellationToken);

                return true;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Document deletion error: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets document content by ID for sharing
    /// </summary>
    public async Task<DocumentContentDto> GetDocumentContentAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await unitOfWork.Documents.GetByIdWithDetailsAsync(id, cancellationToken);

            if (document == null) throw new KeyNotFoundException($"Document not found. ID: {id}");

            logger.LogInformation("Document download - DocumentId: {DocumentId}, StoragePath: {StoragePath}",
                id, document.StoragePath);

            var fileStream = await fileStorageService.DownloadFileAsync(
                document.StorageProviderId.ToString(),
                document.StoragePath,
                cancellationToken);

            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);

            return new DocumentContentDto
            {
                Content = memoryStream.ToArray(),
                FileName = document.FileName,
                ContentType = document.ContentType,
                FileSize = document.FileSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while retrieving document content. Document ID: {DocumentId}", id);
            throw;
        }
    }

    /// <summary>
    /// Downloads document content
    /// </summary>
    public async Task<Stream> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await unitOfWork.Documents.GetByIdWithDetailsAsync(id, cancellationToken);

            if (document == null) throw new KeyNotFoundException($"Document not found. ID: {id}");

            logger.LogInformation("Document download - DocumentId: {DocumentId}, StoragePath: {StoragePath}",
                id, document.StoragePath);

            var fileStream = await fileStorageService.DownloadFileAsync(
                document.StorageProviderId.ToString(),
                document.StoragePath,
                cancellationToken);

            return fileStream;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while downloading document. Document ID: {DocumentId}", id);
            throw;
        }
    }
}
