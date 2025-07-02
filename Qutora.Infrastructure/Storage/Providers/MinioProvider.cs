using Minio;
using Minio.DataModel.Args;
using Microsoft.AspNetCore.Http;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Storage.Models;
using Qutora.Infrastructure.Storage.Registry;
using Microsoft.Extensions.Logging;
using Qutora.Shared.DTOs;
using Qutora.Shared.Enums;

namespace Qutora.Infrastructure.Storage.Providers;

/// <summary>
/// High-performance MinIO storage provider.
/// Provides better performance in high traffic using connection pool.
/// </summary>
[ProviderType("minio")]
public class MinioProvider : BaseStorageProvider, IStorageProviderAdapter, IDisposable
{
    /// <summary>
    /// This provider's type
    /// </summary>
    public static string ProviderTypeValue => "minio";

    public new string ProviderId => _options.ProviderId;
    private readonly MinioProviderOptions _options;
    private readonly MinioConnectionPool _connectionPool;
    private readonly IStorageCapabilityCache _capabilityCache;

    /// <inheritdoc/>
    public override string ProviderType => ProviderTypeValue;

    /// <summary>
    /// Creates provider using Minio connection pool with HttpClientFactory.
    /// </summary>
    public MinioProvider(MinioProviderOptions options, ILogger<MinioProvider> logger,
        IHttpClientFactory httpClientFactory, IStorageCapabilityCache capabilityCache)
        : base(options.ProviderId, logger)
    {
        if (httpClientFactory == null)
            throw new ArgumentNullException(nameof(httpClientFactory));

        _options = options ?? throw new ArgumentNullException(nameof(options));
        _capabilityCache = capabilityCache ?? throw new ArgumentNullException(nameof(capabilityCache));

        if (string.IsNullOrWhiteSpace(_options.Endpoint))
            throw new ArgumentException("MinIO endpoint cannot be empty", nameof(options));

        if (string.IsNullOrWhiteSpace(_options.AccessKey))
            throw new ArgumentException("MinIO access key cannot be empty", nameof(options));

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new ArgumentException("MinIO secret key cannot be empty", nameof(options));

        if (string.IsNullOrWhiteSpace(_options.BucketName))
            throw new ArgumentException("MinIO bucket name cannot be empty", nameof(options));

        _connectionPool = new MinioConnectionPool(options, logger, httpClientFactory);

        _logger.LogInformation("MinIO connection pool created with HttpClientFactory. Endpoint: {Endpoint}",
            _options.Endpoint);
    }

    /// <inheritdoc/>
    public override async Task<string> UploadAsync(string objectKey, Stream content, string contentType)
    {
        try
        {
            var finalObjectKey = string.IsNullOrEmpty(objectKey)
                ? Guid.NewGuid().ToString()
                : objectKey;

            var client = await _connectionPool.GetClientAsync();

            try
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(finalObjectKey)
                    .WithStreamData(content)
                    .WithObjectSize(content.Length)
                    .WithContentType(contentType);

                await client.PutObjectAsync(putObjectArgs);

                LogUploadSuccess(finalObjectKey, content.Length);
                return finalObjectKey;
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error uploading object to MinIO storage: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<Stream> DownloadAsync(string objectKey)
    {
        try
        {
            var client = await _connectionPool.GetClientAsync();

            try
            {
                var (bucket, objectName) = ParseStoragePath(objectKey);

                var memoryStream = new MemoryStream();

                var getObjectArgs = new GetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                    });

                await client.GetObjectAsync(getObjectArgs);

                return new DisposableStreamWrapper(memoryStream, () =>
                {
                    memoryStream.Dispose();
                });
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error downloading object from MinIO storage: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(string objectKey)
    {
        try
        {
            var client = await _connectionPool.GetClientAsync();

            try
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey);

                await client.RemoveObjectAsync(removeObjectArgs);
                _logger.LogInformation("Object successfully deleted: {ObjectKey}", objectKey);
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error deleting object from MinIO storage: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> ExistsAsync(string objectKey)
    {
        try
        {
            var client = await _connectionPool.GetClientAsync();

            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey);

                await client.StatObjectAsync(statObjectArgs);
                return true;
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                return false;
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error checking object existence in MinIO storage: {ObjectKey}", objectKey);
            return false;
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> TestConnectionAsync()
    {
        return await TestConnectionInternalAsync();
    }

    /// <summary>
    /// IStorageProviderAdapter TestConnectionAsync method implementation
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testResult = await TestConnectionInternalAsync(cancellationToken);

            if (testResult)
                return (true, $"S3 connection successful. Endpoint: {_options.Endpoint}, Bucket: {_options.BucketName}");
            else
                return (false, $"Bucket access or creation failed: {_options.BucketName}");
        }
        catch (Minio.Exceptions.MinioException ex) when (ex is Minio.Exceptions.AccessDeniedException)
        {
            return (false,
                $"Access denied. Check your credentials (AccessKey/SecretKey). Details: {ex.Message}");
        }
        catch (Minio.Exceptions.MinioException ex) when (ex is Minio.Exceptions.BucketNotFoundException)
        {
            return (false, $"Bucket not found and could not be created: {_options.BucketName}. Details: {ex.Message}");
        }
        catch (Minio.Exceptions.MinioException ex) when (ex is Minio.Exceptions.ConnectionException)
        {
            return (false,
                $"Connection error. Check your endpoint address and network connection: {_options.Endpoint}. Details: {ex.Message}");
        }
        catch (Minio.Exceptions.MinioException ex)
        {
            return (false, $"MinIO error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during MinIO connection test");
            return (false, $"Unexpected error: {ex.Message}");
        }
    }

    #region IStorageProviderAdapter Implementation

    /// <summary>
    /// Uploads a file
    /// </summary>
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string? contentType = null,
        string? bucketName = null, CancellationToken cancellationToken = default)
    {
        var result = await UploadWithResultAsync(null, fileStream, fileName, Guid.NewGuid().ToString(), contentType,
            bucketName, cancellationToken);
        return result.StoragePath;
    }

    /// <summary>
    /// Uploads a form file
    /// </summary>
    public async Task<string> UploadFileAsync(IFormFile file, string? fileName = null, string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = file.OpenReadStream();
        return await UploadFileAsync(stream, fileName ?? file.FileName, file.ContentType, bucketName,
            cancellationToken);
    }

    /// <summary>
    /// Downloads a file
    /// </summary>
    public Task<Stream> DownloadFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return DownloadAsync(fileName);
    }

    /// <summary>
    /// Deletes a file
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            await DeleteAsync(fileName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a file exists
    /// </summary>
    public Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        return ExistsAsync(fileName);
    }

    /// <summary>
    /// Creates temporary URL for a file
    /// </summary>
    public async Task<string> GetTemporaryUrlAsync(string fileName, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _connectionPool.GetClientAsync();

            try
            {
                var presignedArgs = new PresignedGetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(fileName)
                    .WithExpiry((int)expiry.TotalSeconds);

                return await client.PresignedGetObjectAsync(presignedArgs);
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error generating temporary URL for file: {FileName}", fileName);
            return string.Empty;
        }
    }

    /// <summary>
    /// Method for uploading file and returning result 
    /// </summary>
    public override async Task<UploadResult> UploadWithResultAsync(
        string? objectKey,
        Stream content,
        string fileName,
        string documentId,
        string? contentType = null,
        string? bucketName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var originalPosition = content.Position;

            var fileHash = await GetHashAsync(content);

            content.Position = originalPosition;

            var fileSize = content.Length;

            var finalContentType = EnsureContentType(contentType);

            var finalObjectKey = CreateObjectKey(objectKey, documentId, fileName, bucketName);

            var client = await _connectionPool.GetClientAsync(cancellationToken);

            try
            {
                var targetBucket = _options.BucketName;

                if (!string.IsNullOrEmpty(bucketName))
                {
                    targetBucket = bucketName;
                    _logger.LogInformation("Using custom bucket: {BucketName} instead of default: {DefaultBucket}",
                        bucketName, _options.BucketName);
                }

                var bucketExists = await client.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(targetBucket), cancellationToken);

                if (!bucketExists)
                {
                    _logger.LogInformation("Creating bucket: {BucketName}", targetBucket);
                    await client.MakeBucketAsync(
                        new MakeBucketArgs().WithBucket(targetBucket), cancellationToken);
                }

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(targetBucket)
                    .WithObject(finalObjectKey)
                    .WithStreamData(content)
                    .WithObjectSize(fileSize)
                    .WithContentType(finalContentType);

                await client.PutObjectAsync(putObjectArgs, cancellationToken);

                LogUploadSuccess(finalObjectKey, fileSize);

                return new UploadResult
                {
                    Success = true,
                    StoragePath = $"{targetBucket}/{finalObjectKey}",
                    FileId = documentId,
                    FileName = fileName,
                    ContentType = finalContentType,
                    FileSize = fileSize,
                    FileHash = fileHash,
                    ProviderName = ProviderType,
                    UploadedAt = DateTime.UtcNow
                };
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error uploading file to MinIO storage: {ObjectKey}, {FileName}", objectKey, fileName);

            return new UploadResult
            {
                Success = false,
                FileName = fileName,
                FileId = documentId,
                ErrorMessage = ex.Message,
                ProviderName = ProviderType,
                UploadedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Lists files
    /// </summary>
    public async Task<IEnumerable<string>> ListFilesAsync(string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileNames = new List<string>();
            var client = await _connectionPool.GetClientAsync(cancellationToken);

            try
            {
                var listObjectsArgs = new ListObjectsArgs()
                    .WithBucket(_options.BucketName)
                    .WithPrefix(prefix ?? "")
                    .WithRecursive(true);

                var observable = client.ListObjectsAsync(listObjectsArgs, cancellationToken);
                var tcs = new TaskCompletionSource<bool>();

                observable.Subscribe(
                    item =>
                    {
                        if (!item.IsDir) fileNames.Add(item.Key);
                    },
                    ex =>
                    {
                        _logger.LogError(ex, "Error listing objects from MinIO");
                        tcs.TrySetException(ex);
                    },
                    () => { tcs.TrySetResult(true); });

                await tcs.Task;
                return fileNames;
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error listing files from MinIO storage: {Prefix}", prefix);
            return [];
        }
    }

    /// <summary>
    /// Lists all buckets
    /// </summary>
    /// <returns>List of bucket information</returns>
    public async Task<IEnumerable<BucketInfoDto>> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var buckets = new List<BucketInfoDto>();
            var client = await _connectionPool.GetClientAsync(cancellationToken);

            try
            {
                var bucketList = await client.ListBucketsAsync(cancellationToken);

                foreach (var bucket in bucketList.Buckets)
                {
                    var bucketDto = new BucketInfoDto
                    {
                        Id = Guid.Empty, // No ID for MinIO buckets
                        Path = bucket.Name, // In MinIO, bucket.Name comes from external provider
                        Description = $"MinIO bucket: {bucket.Name}",
                        Permission = PermissionLevel.Read,
                        CreationDate = DateTime.TryParse(bucket.CreationDate, out var parsedDate) ? parsedDate : DateTime.UtcNow,
                        ProviderType = "MinIO",
                        ProviderName = "MinIO",
                        ProviderId = _options.ProviderId.ToString()
                    };
                    buckets.Add(bucketDto);
                }

                return buckets;
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error listing buckets from MinIO");
            return [];
        }
    }

    /// <summary>
    /// Checks if a bucket exists
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if bucket exists, false otherwise</returns>
    public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _connectionPool.GetClientAsync(cancellationToken);

            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);

                return await client.BucketExistsAsync(bucketExistsArgs, cancellationToken);
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error checking if bucket exists: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// Creates a new bucket
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation succeeds, false otherwise</returns>
    public async Task<bool> CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _connectionPool.GetClientAsync(cancellationToken);

            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);

                if (await client.BucketExistsAsync(bucketExistsArgs, cancellationToken))
                {
                    _logger.LogInformation("Bucket already exists: {BucketName}", bucketName);
                    return true;
                }

                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);

                await client.MakeBucketAsync(makeBucketArgs, cancellationToken);

                _logger.LogInformation("Created bucket: {BucketName}", bucketName);
                return true;
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Minio.Exceptions.AccessDeniedException ex)
        {
            _logger.LogError(ex, "Access denied when creating bucket: {BucketName}", bucketName);

            var cacheKey = _capabilityCache.CreateCacheKey(ProviderId);

            _capabilityCache.SetCachedCapability(
                cacheKey,
                Shared.Enums.StorageCapability.BucketCreation,
                false);

            return false;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error creating bucket: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// Deletes a bucket
    /// </summary>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="force">Force delete even if content exists</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if operation succeeds, false otherwise</returns>
    public async Task<bool> RemoveBucketAsync(string bucketName, bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _connectionPool.GetClientAsync(cancellationToken);

            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(bucketName);

                if (!await client.BucketExistsAsync(bucketExistsArgs, cancellationToken))
                {
                    _logger.LogInformation("Bucket does not exist: {BucketName}", bucketName);
                    return true;
                }

                var listObjectsArgs = new ListObjectsArgs()
                    .WithBucket(bucketName)
                    .WithRecursive(true);

                var observable = client.ListObjectsAsync(listObjectsArgs, cancellationToken);
                var hasObjects = false;
                var tcs = new TaskCompletionSource<bool>();

                observable.Subscribe(
                    item =>
                    {
                        hasObjects = true;
                        tcs.TrySetResult(true);
                    },
                    ex => { tcs.TrySetException(ex); },
                    () =>
                    {
                        if (!hasObjects)
                            tcs.TrySetResult(false);
                    });

                await tcs.Task;

                switch (hasObjects)
                {
                    case true when !force:
                        _logger.LogWarning("Cannot remove non-empty bucket without force parameter: {BucketName}",
                            bucketName);
                        return false;
                    case true when force:
                        await RemoveAllObjectsInBucketAsync(bucketName, client, cancellationToken);
                        break;
                }

                var removeBucketArgs = new RemoveBucketArgs()
                    .WithBucket(bucketName);

                await client.RemoveBucketAsync(removeBucketArgs, cancellationToken);

                _logger.LogInformation("Removed bucket: {BucketName}", bucketName);
                return true;
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error removing bucket: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// Deletes all objects in bucket
    /// </summary>
    private async Task RemoveAllObjectsInBucketAsync(string bucketName, IMinioClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithRecursive(true);

            var observable = client.ListObjectsAsync(listObjectsArgs, cancellationToken);
            var objectsToDelete = new List<string>();
            var tcs = new TaskCompletionSource<bool>();

            observable.Subscribe(
                item => { objectsToDelete.Add(item.Key); },
                ex =>
                {
                    _logger.LogError(ex, "Error listing objects for deletion in bucket: {BucketName}", bucketName);
                    tcs.TrySetException(ex);
                },
                () => { tcs.TrySetResult(true); });

            await tcs.Task;

            foreach (var objectKey in objectsToDelete)
            {
                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectKey);

                await client.RemoveObjectAsync(removeObjectArgs, cancellationToken);
            }

            _logger.LogInformation("Removed {Count} objects from bucket: {BucketName}",
                objectsToDelete.Count, bucketName);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error removing objects from bucket: {BucketName}", bucketName);
            throw;
        }
    }

    /// <summary>
    /// Internal method for connection testing
    /// </summary>
    private async Task<bool> TestConnectionInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await _connectionPool.GetClientAsync(cancellationToken);

            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_options.BucketName);

                var found = await client.BucketExistsAsync(bucketExistsArgs, cancellationToken);

                if (!found)
                    try
                    {
                        var makeBucketArgs = new MakeBucketArgs()
                            .WithBucket(_options.BucketName);

                        await client.MakeBucketAsync(makeBucketArgs, cancellationToken);
                        _logger.LogInformation("Created bucket: {BucketName}", _options.BucketName);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Error creating bucket: {BucketName}", _options.BucketName);
                        return false;
                    }

                return true;
            }
            finally
            {
                _connectionPool.ReturnClient(client);
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error testing MinIO connection: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Parses StoragePath to separate bucket and object name
    /// </summary>
    /// <param name="storagePath">Storage path to parse</param>
    /// <returns>Tuple of bucket name and object name</returns>
    private (string bucket, string objectName) ParseStoragePath(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath))
            throw new ArgumentException("Storage path cannot be null or empty");

        if (storagePath.Contains('/'))
        {
            var parts = storagePath.Split('/', 2);
            return (parts[0], parts[1]);
        }

        _logger.LogWarning("Storage path without bucket detected: {StoragePath}. Using default bucket: {DefaultBucket}",
            storagePath, _options.BucketName);
        return (_options.BucketName, storagePath);
    }

    #endregion

    /// <summary>
    /// Releases all resources except the class for Minio
    /// </summary>
    public void Dispose()
    {
        _connectionPool.Dispose();
        GC.SuppressFinalize(this);
    }
}
