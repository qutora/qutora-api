using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Storage.Models;
using Qutora.Infrastructure.Storage.Registry;
using Qutora.Shared.DTOs;

namespace Qutora.Infrastructure.Storage.Providers;

/// <summary>
/// Local file system storage provider implementation.
/// </summary>
[ProviderType("filesystem")]
public class FileSystemProvider : BaseStorageProvider, IStorageProviderAdapter
{
    /// <summary>
    /// This provider's type
    /// </summary>
    public static string ProviderTypeValue => "filesystem";

    public new string ProviderId => _options.ProviderId;

    private readonly FileSystemProviderOptions _options;
    private readonly IStorageCapabilityCache _capabilityCache;

    public FileSystemProvider(FileSystemProviderOptions options, ILogger<FileSystemProvider> logger,
        IStorageCapabilityCache capabilityCache)
        : base(options.ProviderId, logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _capabilityCache = capabilityCache ?? throw new ArgumentNullException(nameof(capabilityCache));

        if (!Directory.Exists(_options.RootPath))
        {
            Directory.CreateDirectory(_options.RootPath);
            _logger.LogInformation("Created root storage directory: {RootPath}", _options.RootPath);
        }
    }

    /// <inheritdoc/>
    public override string ProviderType => ProviderTypeValue;

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
    /// Lists files with optional prefix
    /// </summary>
    public async Task<IEnumerable<string>> ListFilesAsync(string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prefixPath = prefix == null ? _options.RootPath : Path.Combine(_options.RootPath, prefix);

            var files = new List<string>();

            if (Directory.Exists(prefixPath))
            {
                var allFiles = Directory.GetFiles(prefixPath, "*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    var relativePath = file.Substring(_options.RootPath.Length).TrimStart(Path.DirectorySeparatorChar);
                    files.Add(relativePath.Replace(Path.DirectorySeparatorChar, '/'));
                }
            }

            return files;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error listing files in file system provider: {Prefix}", prefix);
            return [];
        }
    }

    /// <summary>
    /// Creates a temporary URL for a file
    /// </summary>
    public Task<string> GetTemporaryUrlAsync(string fileName, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("GetTemporaryUrlAsync is not applicable for file system storage");
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Tests the connection
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testResult = await TestConnectionInternalAsync();

            if (testResult)
                return (true, $"File system connection successful. Path: {_options.RootPath}");
            else
                return (false, $"Unable to access or write to path: {_options.RootPath}");
        }
        catch (Exception ex)
        {
            return (false, $"File system connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests the connection
    /// </summary>
    /// <returns>Test result</returns>
    public override async Task<bool> TestConnectionAsync()
    {
        return await TestConnectionInternalAsync();
    }

    /// <summary>
    /// Internal method for connection testing
    /// </summary>
    private async Task<bool> TestConnectionInternalAsync()
    {
        try
        {
            if (!Directory.Exists(_options.RootPath))
                try
                {
                    Directory.CreateDirectory(_options.RootPath);
                }
                catch
                {
                    return false;
                }

            var testFilePath = Path.Combine(_options.RootPath, $"test_{Guid.NewGuid()}.tmp");
            try
            {
                await File.WriteAllTextAsync(testFilePath, "Connection Test");
                File.Delete(testFilePath);
                return true;
            }
            catch
            {
                if (File.Exists(testFilePath))
                    try
                    {
                        File.Delete(testFilePath);
                    }
                    catch
                    {
                    }

                return false;
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error testing filesystem provider connection");
            return false;
        }
    }

    /// <inheritdoc/>
    public override async Task<string> UploadAsync(string objectKey, Stream content, string contentType)
    {
        try
        {
            var finalObjectKey = string.IsNullOrEmpty(objectKey)
                ? Guid.NewGuid().ToString()
                : objectKey;

            var filePath = Path.Combine(_options.RootPath, finalObjectKey);

            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await content.CopyToAsync(fileStream);
            }

            LogUploadSuccess(finalObjectKey, content.Length);

            return finalObjectKey;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error uploading file to file system: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<UploadResult> UploadWithResultAsync(string? objectKey, Stream content, string fileName,
        string documentId, string? contentType = null, string? bucketName = null,
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

            var normalizedObjectKey = finalObjectKey.Replace('/', Path.DirectorySeparatorChar);
            string filePath;

            if (!string.IsNullOrEmpty(bucketName))
            {
                var bucketPath = Path.Combine(_options.RootPath, bucketName);

                if (!Directory.Exists(bucketPath))
                {
                    Directory.CreateDirectory(bucketPath);
                    _logger.LogInformation("Created bucket directory: {BucketPath}", bucketPath);
                }

                filePath = Path.Combine(bucketPath, normalizedObjectKey);
            }
            else
            {
                filePath = Path.Combine(_options.RootPath, normalizedObjectKey);
            }

            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            LogUploadSuccess(finalObjectKey, fileSize);

            return new UploadResult
            {
                Success = true,
                StoragePath =
                    (!string.IsNullOrEmpty(bucketName) ? $"{bucketName}/{finalObjectKey}" : $"default/{finalObjectKey}")
                    .Replace('\\', '/'),
                FileId = documentId,
                FileName = fileName,
                ContentType = finalContentType,
                FileSize = fileSize,
                FileHash = fileHash,
                ProviderName = ProviderType,
                UploadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            LogError(ex, "Error uploading file to file system: {ObjectKey}, {FileName}", objectKey, fileName);

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

    /// <inheritdoc/>
    public override async Task<Stream> DownloadAsync(string objectKey)
    {
        try
        {
            var normalizedPath = objectKey.Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_options.RootPath, normalizedPath);

            if (!fullPath.StartsWith(_options.RootPath))
                throw new UnauthorizedAccessException("Access to path outside root directory is not allowed");

            if (!File.Exists(fullPath)) throw new FileNotFoundException($"File not found: {objectKey}", fullPath);

            var memoryStream = new MemoryStream();
            await using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                await fileStream.CopyToAsync(memoryStream);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error downloading file from file system: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(string objectKey)
    {
        try
        {
            var filePath = Path.Combine(_options.RootPath, objectKey);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                _logger.LogInformation("File deleted from file system: {ObjectKey}, Path: {FilePath}",
                    objectKey, filePath);
            }
            else
            {
                _logger.LogWarning("File not found for deletion: {ObjectKey}, Path: {FilePath}",
                    objectKey, filePath);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error deleting file from file system: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> ExistsAsync(string objectKey)
    {
        var filePath = Path.Combine(_options.RootPath, objectKey);
        var exists = File.Exists(filePath);

        await Task.CompletedTask;
        return exists;
    }

    /// <summary>
    /// Lists all buckets/folders
    /// </summary>
    public async Task<IEnumerable<BucketInfoDto>> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var buckets = new List<BucketInfoDto>();

            if (Directory.Exists(_options.RootPath))
            {
                var directories = Directory.GetDirectories(_options.RootPath);

                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var bucketName = dirInfo.Name;

                    long size = 0;
                    var objectCount = 0;

                    try
                    {
                        var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                        objectCount = files.Count();

                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);
                            size += fileInfo.Length;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Error calculating size/objects for bucket: {BucketName}", bucketName);
                    }

                    buckets.Add(new BucketInfoDto
                    {
                        Id = CreateDeterministicGuid(ProviderId, bucketName),
                        Path = bucketName,
                        CreationDate = dirInfo.CreationTimeUtc,
                        Size = size > 0 ? size : null,
                        ObjectCount = objectCount > 0 ? objectCount : null,
                        ProviderType = ProviderTypeValue,
                        ProviderName = "FileSystem",
                        ProviderId = ProviderId
                    });
                }
            }

            await Task.CompletedTask;
            return buckets;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error listing buckets in file system");
            return [];
        }
    }

    /// <summary>
    /// Checks if a bucket/folder exists
    /// </summary>
    public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(bucketName)) return false;

            var bucketPath = Path.Combine(_options.RootPath, bucketName);
            var exists = Directory.Exists(bucketPath);

            await Task.CompletedTask;
            return exists;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error checking bucket existence: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// Creates a new bucket/folder
    /// </summary>
    public async Task<bool> CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(bucketName)) return false;

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                if (bucketName.Contains(invalidChar))
                {
                    _logger.LogWarning("Invalid character in bucket name: {BucketName}", bucketName);
                    return false;
                }

            var bucketPath = Path.Combine(_options.RootPath, bucketName);

            if (Directory.Exists(bucketPath))
            {
                _logger.LogWarning("Bucket already exists: {BucketName}", bucketName);
                return false;
            }

            Directory.CreateDirectory(bucketPath);
            _logger.LogInformation("Created bucket: {BucketName} at {Path}", bucketName, bucketPath);

            await Task.CompletedTask;
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when creating bucket in filesystem: {BucketName}", bucketName);

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
    /// Deletes a bucket/folder
    /// </summary>
    public async Task<bool> RemoveBucketAsync(string bucketName, bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(bucketName)) return false;

            var bucketPath = Path.Combine(_options.RootPath, bucketName);

            if (!Directory.Exists(bucketPath))
            {
                _logger.LogWarning("Bucket does not exist: {BucketName}", bucketName);
                return false;
            }

            if (!force)
            {
                var isEmpty = !Directory.EnumerateFileSystemEntries(bucketPath).Any();
                if (!isEmpty)
                {
                    _logger.LogWarning("Bucket is not empty and force=false: {BucketName}", bucketName);
                    return false;
                }
            }

            Directory.Delete(bucketPath, force);
            _logger.LogInformation("Removed bucket: {BucketName}", bucketName);

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error removing bucket: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// Determines bucket mapping key for FileSystem provider
    /// Uses Path field as the single source of truth
    /// </summary>
    public override string GetBucketSearchKey(StorageBucket bucket)
    {
        return bucket.Path;
    }

    private static Guid CreateDeterministicGuid(string providerId, string bucketName)
    {
        var input = $"{providerId}:{bucketName}";
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash);
        }
    }
}
