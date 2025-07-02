using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Storage.Models;
using Qutora.Infrastructure.Storage.Registry;
using FluentFTP;
using Qutora.Shared.DTOs;

namespace Qutora.Infrastructure.Storage.Providers;

/// <summary>
/// FTP storage provider implementation.
/// </summary>
[ProviderType("ftp")]
public class FtpProvider : BaseStorageProvider, IStorageProviderAdapter
{
    /// <summary>
    /// FTP provider type
    /// </summary>
    public static string ProviderTypeValue => "ftp";

    public new string ProviderId => _options.ProviderId;

    private readonly FtpProviderOptions _options;
    private readonly IStorageCapabilityCache _capabilityCache;

    public FtpProvider(FtpProviderOptions options, ILogger<FtpProvider> logger, IStorageCapabilityCache capabilityCache)
        : base(options.ProviderId, logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _capabilityCache = capabilityCache ?? throw new ArgumentNullException(nameof(capabilityCache));
    }

    /// <inheritdoc/>
    public override string ProviderType => ProviderTypeValue;

    /// <summary>
    /// Creates FTP connection
    /// </summary>
    private AsyncFtpClient CreateFtpClient()
    {
        var config = new FtpConfig
        {
            RetryAttempts = 3,
            ConnectTimeout = 30000,
            ReadTimeout = 30000,
            DataConnectionConnectTimeout = 30000,
            DataConnectionReadTimeout = 30000,
            LogToConsole = false,
            ValidateAnyCertificate = true
        };

        var client = new AsyncFtpClient(
            _options.Host,
            _options.Username,
            _options.Password,
            _options.Port,
            config
        );

        if (_options.UseSsl)
        {
            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
            client.Config.ValidateAnyCertificate = true;
        }

        if (_options.UsePassiveMode)
            client.Config.DataConnectionType = FtpDataConnectionType.PASV;
        else
            client.Config.DataConnectionType = FtpDataConnectionType.PORT;

        return client;
    }

    /// <inheritdoc/>
    public override async Task<string> UploadAsync(string objectKey, Stream content, string contentType)
    {
        try
        {
            var finalObjectKey = string.IsNullOrEmpty(objectKey)
                ? $"{Guid.NewGuid():N}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : objectKey;

            var remotePath = Path.Combine(_options.RootDirectory, finalObjectKey)
                .Replace('\\', '/');

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            var directoryPath = Path.GetDirectoryName(remotePath)?.Replace('\\', '/') ?? string.Empty;
            if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
                await ftpClient.CreateDirectory(directoryPath);

            var ftpStatus = await ftpClient.UploadStream(content, remotePath, FtpRemoteExists.Overwrite);
            await ftpClient.Disconnect();

            if (ftpStatus != FtpStatus.Success) throw new IOException($"FTP upload failed with status: {ftpStatus}");

            LogUploadSuccess(finalObjectKey, content.Length);

            return finalObjectKey;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error uploading file to FTP storage: {ObjectKey}", objectKey);
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

            string remotePath;
            if (!string.IsNullOrEmpty(bucketName))
            {
                // Include bucket path if bucket name exists
                var bucketPath = Path.Combine(_options.RootDirectory, bucketName);
                remotePath = Path.Combine(bucketPath, finalObjectKey).Replace('\\', '/');
            }
            else
            {
                // Use root directory directly if no bucket name
                remotePath = Path.Combine(_options.RootDirectory, finalObjectKey).Replace('\\', '/');
            }

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            var directoryPath = Path.GetDirectoryName(remotePath)?.Replace('\\', '/') ?? string.Empty;
            if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
                await ftpClient.CreateDirectory(directoryPath);

            var ftpStatus = await ftpClient.UploadStream(content, remotePath, FtpRemoteExists.Overwrite);
            await ftpClient.Disconnect();

            if (ftpStatus != FtpStatus.Success) throw new IOException($"FTP upload failed with status: {ftpStatus}");

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
            LogError(ex, "Error uploading file to FTP storage: {ObjectKey}, {FileName}", objectKey, fileName);

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
            var remotePath = Path.Combine(_options.RootDirectory, normalizedPath)
                .Replace('\\', '/');

            var memoryStream = new MemoryStream();

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            var downloadSuccess = await ftpClient.DownloadStream(memoryStream, remotePath);
            await ftpClient.Disconnect();

            if (!downloadSuccess) throw new IOException($"FTP download failed");

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error downloading file from FTP storage: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task DeleteAsync(string objectKey)
    {
        try
        {
            var remotePath = Path.Combine(_options.RootDirectory, objectKey)
                .Replace('\\', '/');

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            await ftpClient.DeleteFile(remotePath);
            await ftpClient.Disconnect();

            _logger.LogInformation("File deleted from FTP storage: {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error deleting file from FTP storage: {ObjectKey}", objectKey);
            throw;
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> ExistsAsync(string objectKey)
    {
        try
        {
            var remotePath = Path.Combine(_options.RootDirectory, objectKey)
                .Replace('\\', '/');

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            var exists = await ftpClient.FileExists(remotePath);
            await ftpClient.Disconnect();

            return exists;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
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
            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();
            var isConnected = ftpClient.IsConnected;
            await ftpClient.Disconnect();
            return isConnected;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error testing FTP connection: {Message}", ex.Message);
            return false;
        }
    }

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
    /// Lists files
    /// </summary>
    public async Task<IEnumerable<string>> ListFilesAsync(string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileNames = new List<string>();

            var remotePath = string.IsNullOrEmpty(prefix)
                ? _options.RootDirectory
                : Path.Combine(_options.RootDirectory, prefix)
                    .Replace('\\', '/');

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            FtpListItem[] listing = await ftpClient.GetListing(remotePath);

            var rootLength = _options.RootDirectory.EndsWith('/')
                ? _options.RootDirectory.Length
                : _options.RootDirectory.Length + 1;

            foreach (var item in listing)
                if (item.Type == FtpObjectType.File)
                {
                    var fullPath = item.FullName.Replace('\\', '/');

                    if (fullPath.StartsWith(_options.RootDirectory))
                    {
                        var relativePath = fullPath.Substring(rootLength);
                        fileNames.Add(relativePath);
                    }
                    else
                    {
                        fileNames.Add(fullPath);
                    }
                }

            await ftpClient.Disconnect();

            _logger.LogInformation("Listed {Count} files from FTP storage", fileNames.Count);
            return fileNames;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error listing files in FTP storage: {Prefix}", prefix);
            return [];
        }
    }

    /// <summary>
    /// Creates temporary URL for a file
    /// </summary>
    public Task<string> GetTemporaryUrlAsync(string fileName, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("FTP provider does not support temporary URLs");
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// For connection testing
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testResult = await TestConnectionInternalAsync();

            if (testResult)
                return (true, $"FTP connection successful. Host: {_options.Host}, Port: {_options.Port}");
            else
                return (false, $"Unable to connect to FTP server: {_options.Host}:{_options.Port}");
        }
        catch (Exception ex)
        {
            return (false, $"FTP connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Lists all buckets/folders
    /// </summary>
    public async Task<IEnumerable<BucketInfoDto>> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var buckets = new List<BucketInfoDto>();

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            var rootPath = _options.RootDirectory;
            var listing = await ftpClient.GetListing(rootPath);

            foreach (var item in listing)
                if (item.Type == FtpObjectType.Directory)
                {
                    var bucketName = Path.GetFileName(item.FullName);
                    var creationDate = item.Modified;

                    long size = 0;
                    var objectCount = 0;

                    try
                    {
                        if (_options.CalculateBucketSize)
                        {
                            var fileList = await ftpClient.GetListing(item.FullName, FtpListOption.Recursive);
                            objectCount = fileList.Count(f => f.Type == FtpObjectType.File);

                            foreach (var file in fileList)
                                if (file.Type == FtpObjectType.File)
                                    size += file.Size;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Error calculating size for bucket: {BucketName}", bucketName);
                    }

                    buckets.Add(new BucketInfoDto
                    {
                        Path = bucketName,
                        CreationDate = creationDate,
                        Size = size > 0 ? size : null,
                        ObjectCount = objectCount > 0 ? objectCount : null,
                        ProviderType = ProviderTypeValue,
                        ProviderName = "FTP",
                        ProviderId = ProviderId
                    });
                }

            await ftpClient.Disconnect();

            _logger.LogInformation("Listed {Count} buckets from FTP storage", buckets.Count);
            return buckets;
        }
        catch (Exception ex)
        {
            LogError(ex, "Error listing buckets from FTP storage");
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

            var bucketPath = Path.Combine(_options.RootDirectory, bucketName)
                .Replace('\\', '/');

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            var exists = await ftpClient.DirectoryExists(bucketPath);

            await ftpClient.Disconnect();

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

            var bucketPath = Path.Combine(_options.RootDirectory, bucketName)
                .Replace('\\', '/');

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            if (await ftpClient.DirectoryExists(bucketPath))
            {
                _logger.LogInformation("Bucket already exists: {BucketName}", bucketName);
                await ftpClient.Disconnect();
                return true;
            }

            var result = await ftpClient.CreateDirectory(bucketPath);

            await ftpClient.Disconnect();

            if (result)
            {
                _logger.LogInformation("Created bucket: {BucketName}", bucketName);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to create bucket: {BucketName}", bucketName);
                return false;
            }
        }
        catch (FluentFTP.Exceptions.FtpCommandException ex) when (ex.Message.Contains("Permission denied") ||
                                                                  ex.Message.Contains("Access denied") ||
                                                                  ex.Message.ToLower().Contains("not permitted"))
        {
            _logger.LogError(ex, "Access denied when creating bucket in FTP: {BucketName}", bucketName);

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

            var bucketPath = Path.Combine(_options.RootDirectory, bucketName)
                .Replace('\\', '/');

            await using var ftpClient = CreateFtpClient();
            await ftpClient.Connect();

            if (!await ftpClient.DirectoryExists(bucketPath))
            {
                _logger.LogInformation("Bucket does not exist: {BucketName}", bucketName);
                await ftpClient.Disconnect();
                return true;
            }

            if (!force)
            {
                var listing = await ftpClient.GetListing(bucketPath);
                var isEmpty = listing.Length == 0;

                if (!isEmpty)
                {
                    _logger.LogWarning("Cannot remove non-empty bucket without force parameter: {BucketName}",
                        bucketName);
                    await ftpClient.Disconnect();
                    return false;
                }
            }

            try
            {
                await ftpClient.DeleteDirectory(bucketPath, force ? FtpListOption.Recursive : FtpListOption.Auto);
                await ftpClient.Disconnect();

                _logger.LogInformation("Removed bucket: {BucketName}", bucketName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to remove bucket: {BucketName}, Error: {Error}",
                    bucketName, ex.Message);
                await ftpClient.Disconnect();
                return false;
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Error removing bucket: {BucketName}", bucketName);
            return false;
        }
    }
}
