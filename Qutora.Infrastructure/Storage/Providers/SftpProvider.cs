using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Qutora.Infrastructure.Storage.Models;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Storage.Registry;
using Renci.SshNet;
using Qutora.Shared.DTOs;

namespace Qutora.Infrastructure.Storage.Providers;

/// <summary>
/// SFTP storage provider
/// </summary>
[ProviderType("sftp")]
public class SftpProvider : BaseStorageProvider, IStorageProviderAdapter
{
    /// <summary>
    /// This provider's type
    /// </summary>
    public static string ProviderTypeValue => "sftp";

    public new string ProviderId => _options.ProviderId;

    private readonly SftpProviderOptions _options;
    private readonly IStorageCapabilityCache _capabilityCache;

    public SftpProvider(SftpProviderOptions options, ILogger<SftpProvider> logger,
        IStorageCapabilityCache capabilityCache)
        : base(options.ProviderId, logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _capabilityCache = capabilityCache ?? throw new ArgumentNullException(nameof(capabilityCache));
    }

    /// <inheritdoc/>
    public override string ProviderType => ProviderTypeValue;

    /// <summary>
    /// Creates SFTP client
    /// </summary>
    private SftpClient CreateSftpClient()
    {
        if (!string.IsNullOrEmpty(_options.PrivateKeyPath))
        {
            var privateKeyAuth = new PrivateKeyAuthenticationMethod(
                _options.Username,
                new PrivateKeyFile(_options.PrivateKeyPath, _options.Passphrase)
            );

            var sshConnectionInfo = new Renci.SshNet.ConnectionInfo(
                _options.Host,
                _options.Port,
                _options.Username,
                privateKeyAuth
            );

            return new SftpClient(sshConnectionInfo);
        }
        else
        {
            return new SftpClient(
                _options.Host,
                _options.Port,
                _options.Username,
                _options.Password
            );
        }
    }

    /// <summary>
    /// Creates directories recursively
    /// </summary>
    private void CreateDirectoryRecursive(SftpClient client, string path)
    {
        if (path == "/" || string.IsNullOrEmpty(path))
            return;

        if (!client.Exists(path))
        {
            var parentPath = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
            CreateDirectoryRecursive(client, parentPath);

            client.CreateDirectory(path);
            _logger.LogDebug("SFTP directory created: {Path}", path);
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
    /// Lists files with optional prefix
    /// </summary>
    public async Task<IEnumerable<string>> ListFilesAsync(string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileNames = new List<string>();

            var remotePath = string.IsNullOrEmpty(prefix)
                ? _options.RootPath
                : Path.Combine(_options.RootPath, prefix).Replace('\\', '/');

            using (var client = CreateSftpClient())
            {
                client.Connect();

                if (!client.Exists(remotePath))
                {
                    _logger.LogWarning("Directory not found for listing: {Path}", remotePath);
                    return fileNames;
                }

                if (!client.GetAttributes(remotePath).IsDirectory)
                {
                    if (remotePath.StartsWith(_options.RootPath))
                    {
                        var relativePath = remotePath.Substring(_options.RootPath.Length).TrimStart('/');
                        fileNames.Add(relativePath);
                    }

                    return fileNames;
                }

                await Task.Run(() => ListFilesRecursive(client, remotePath, fileNames, cancellationToken),
                    cancellationToken);

                client.Disconnect();
            }

            _logger.LogInformation("Listed {Count} files via SFTP", fileNames.Count);
            return fileNames;
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP file listing error: {Prefix}", prefix);
            return [];
        }
    }

    /// <summary>
    /// Lists files recursively
    /// </summary>
    private void ListFilesRecursive(SftpClient client, string path, List<string> fileList,
        CancellationToken cancellationToken)
    {
        try
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            foreach (var item in client.ListDirectory(path))
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (item.Name == "." || item.Name == "..")
                    continue;

                if (item.IsDirectory)
                {
                    ListFilesRecursive(client, item.FullName, fileList, cancellationToken);
                }
                else
                {
                    if (item.FullName.StartsWith(_options.RootPath))
                    {
                        var relativePath = item.FullName.Substring(_options.RootPath.Length).TrimStart('/');
                        fileList.Add(relativePath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error occurred while listing directory: {Path}", path);
        }
    }

    /// <summary>
    /// Creates temporary URL for a file - not supported for SFTP
    /// </summary>
    public Task<string> GetTemporaryUrlAsync(string fileName, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SFTP provider does not support temporary URLs");
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// For connection testing
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing SFTP connection: {Host}:{Port}", _options.Host, _options.Port);

            if (string.IsNullOrEmpty(_options.Host)) return (false, "SFTP server address not specified");

            if (string.IsNullOrEmpty(_options.Username)) return (false, "SFTP username not specified");

            if (string.IsNullOrEmpty(_options.Password) && string.IsNullOrEmpty(_options.PrivateKeyPath))
                return (false, "SFTP password or private key not specified");

            var testResult = await TestConnectionInternalAsync();

            if (testResult)
                return (true, $"SFTP connection successful. Host: {_options.Host}, Port: {_options.Port}");
            else
                return (false, $"SFTP connection failed: {_options.Host}:{_options.Port}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SFTP connection test failed: {Message}", ex.Message);
            return (false, $"SFTP connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests the connection
    /// </summary>
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
            using var client = CreateSftpClient();

            await Task.Run(() =>
            {
                client.Connect();
                client.Disconnect();
            });

            return true;
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP connection test failed: {Message}", ex.Message);
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

            var remotePath = Path.Combine(_options.RootPath, finalObjectKey)
                .Replace('\\', '/');

            await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                var directoryPath = Path.GetDirectoryName(remotePath)?.Replace('\\', '/') ?? string.Empty;
                if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
                    CreateDirectoryRecursive(client, directoryPath);

                using var fileStream = client.Create(remotePath);
                content.CopyTo(fileStream);

                client.Disconnect();
            });

            LogUploadSuccess(finalObjectKey, content.Length);

            return finalObjectKey;
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP file upload error: {ObjectKey}", objectKey);
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
                var bucketPath = Path.Combine(_options.RootPath, bucketName);
                remotePath = Path.Combine(bucketPath, finalObjectKey).Replace('\\', '/');
            }
            else
            {
                // Use root directory directly if no bucket name
                remotePath = Path.Combine(_options.RootPath, finalObjectKey).Replace('\\', '/');
            }

            await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                var directoryPath = Path.GetDirectoryName(remotePath)?.Replace('\\', '/') ?? string.Empty;
                if (!string.IsNullOrEmpty(directoryPath) && directoryPath != "/")
                    CreateDirectoryRecursive(client, directoryPath);

                using var fileStream = client.Create(remotePath);
                content.CopyTo(fileStream);

                client.Disconnect();
            }, cancellationToken);

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
            LogError(ex, "SFTP file upload error: {ObjectKey}, {FileName}", objectKey, fileName);

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

    public override async Task<Stream> DownloadAsync(string objectKey)
    {
        try
        {
            var normalizedPath = objectKey.Replace('/', Path.DirectorySeparatorChar);
            var remotePath = Path.Combine(_options.RootPath, normalizedPath)
                .Replace('\\', '/');

            var memoryStream = new MemoryStream();

            await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                if (!client.Exists(remotePath))
                    throw new FileNotFoundException($"File not found on SFTP server: {remotePath}");

                if (!client.GetAttributes(remotePath).IsDirectory)
                {
                    using var sourceStream = client.OpenRead(remotePath);
                    sourceStream.CopyTo(memoryStream);
                }
                else
                {
                    throw new InvalidOperationException($"Specified path is not a file, but a directory: {remotePath}");
                }

                client.Disconnect();
            });

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP file download error: {ObjectKey}", objectKey);
            throw;
        }
    }

    public override async Task DeleteAsync(string objectKey)
    {
        try
        {
            var remotePath = Path.Combine(_options.RootPath, objectKey)
                .Replace('\\', '/');

            await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                if (client.Exists(remotePath))
                {
                    if (!client.GetAttributes(remotePath).IsDirectory)
                        client.DeleteFile(remotePath);
                    else
                        throw new InvalidOperationException($"Specified path is not a file, but a directory: {remotePath}");
                }
                else
                {
                    _logger.LogWarning("File to delete not found on SFTP server: {Path}", remotePath);
                }

                client.Disconnect();
            });

            _logger.LogInformation("File SFTP server deleted: {ObjectKey}", objectKey);
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP file delete error: {ObjectKey}", objectKey);
            throw;
        }
    }

    public override async Task<bool> ExistsAsync(string objectKey)
    {
        try
        {
            var remotePath = Path.Combine(_options.RootPath, objectKey)
                .Replace('\\', '/');

            return await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                var exists = client.Exists(remotePath);

                if (exists && client.GetAttributes(remotePath).IsDirectory) exists = false;

                client.Disconnect();
                return exists;
            });
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP file existence check error: {ObjectKey}", objectKey);
            return false;
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

            await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                if (!client.Exists(_options.RootPath))
                {
                    _logger.LogWarning("SFTP root directory not found: {RootPath}", _options.RootPath);
                    client.Disconnect();
                    return;
                }

                var directories = client.ListDirectory(_options.RootPath);

                foreach (var directory in directories)
                {
                    if (directory.Name == "." || directory.Name == "..")
                        continue;

                    if (directory.IsDirectory)
                    {
                        var bucketName = directory.Name;
                        var creationDate = directory.LastWriteTimeUtc;

                        long size = 0;
                        var objectCount = 0;

                        if (_options.CalculateBucketSizes)
                            try
                            {
                                var bucketStats = CalculateBucketStats(client, directory.FullName);
                                size = bucketStats.TotalSize;
                                objectCount = bucketStats.FileCount;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Bucket size calculation error: {BucketName}", bucketName);
                            }

                        buckets.Add(new BucketInfoDto
                        {
                            Path = bucketName,
                            CreationDate = creationDate,
                            Size = size > 0 ? size : null,
                            ObjectCount = objectCount > 0 ? objectCount : null,
                            ProviderType = ProviderTypeValue,
                            ProviderName = "SFTP",
                            ProviderId = ProviderId
                        });
                    }
                }

                client.Disconnect();
            }, cancellationToken);

            _logger.LogInformation("Listed {Count} buckets via SFTP", buckets.Count);
            return buckets;
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP bucket listing error");
            return [];
        }
    }

    /// <summary>
    /// Calculates file count and total size in bucket
    /// </summary>
    private (long TotalSize, int FileCount) CalculateBucketStats(SftpClient client, string directoryPath)
    {
        long totalSize = 0;
        var fileCount = 0;

        var files = client.ListDirectory(directoryPath);

        foreach (var file in files)
        {
            if (file.Name == "." || file.Name == "..")
                continue;

            if (file.IsDirectory)
            {
                var subdirStats = CalculateBucketStats(client, file.FullName);
                totalSize += subdirStats.TotalSize;
                fileCount += subdirStats.FileCount;
            }
            else if (file.IsRegularFile)
            {
                totalSize += file.Length;
                fileCount++;
            }
        }

        return (totalSize, fileCount);
    }

    /// <summary>
    /// Checks if a bucket/folder exists
    /// </summary>
    public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(bucketName)) return false;

            var bucketPath = Path.Combine(_options.RootPath, bucketName)
                .Replace('\\', '/');

            return await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                var exists = client.Exists(bucketPath) && client.GetAttributes(bucketPath).IsDirectory;

                client.Disconnect();
                return exists;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP bucket existence check error: {BucketName}", bucketName);
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

            var bucketPath = Path.Combine(_options.RootPath, bucketName)
                .Replace('\\', '/');

            return await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                if (client.Exists(bucketPath))
                {
                    if (client.GetAttributes(bucketPath).IsDirectory)
                    {
                        _logger.LogInformation("Bucket already exists: {BucketName}", bucketName);
                        client.Disconnect();
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Bucket creation failed, a file with the same name exists: {BucketName}",
                            bucketName);
                        client.Disconnect();
                        return false;
                    }
                }

                try
                {
                    client.CreateDirectory(bucketPath);

                    _logger.LogInformation("Bucket created: {BucketName}", bucketName);
                    client.Disconnect();
                    return true;
                }
                catch (Renci.SshNet.Common.SshConnectionException ex) when (ex.Message.Contains("Permission denied") ||
                                                                            ex.Message.Contains("Access denied"))
                {
                    _logger.LogError(ex, "SFTP bucket creation permission denied: {BucketName}", bucketName);

                    var cacheKey = _capabilityCache.CreateCacheKey(ProviderId);

                    _capabilityCache.SetCachedCapability(
                        cacheKey,
                        Shared.Enums.StorageCapability.BucketCreation,
                        false);

                    client.Disconnect();
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SFTP bucket creation error: {BucketName}", bucketName);
                    client.Disconnect();
                    return false;
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP bucket creation error: {BucketName}", bucketName);
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

            var bucketPath = Path.Combine(_options.RootPath, bucketName)
                .Replace('\\', '/');

            return await Task.Run(() =>
            {
                using var client = CreateSftpClient();
                client.Connect();

                if (!client.Exists(bucketPath))
                {
                    _logger.LogInformation("Bucket to delete not found: {BucketName}", bucketName);
                    client.Disconnect();
                    return true;
                }

                if (!client.GetAttributes(bucketPath).IsDirectory)
                {
                    _logger.LogWarning("Specified path is not a folder: {BucketName}", bucketName);
                    client.Disconnect();
                    return false;
                }

                var directoryItems = client.ListDirectory(bucketPath);
                var isEmpty = true;

                foreach (var item in directoryItems)
                    if (item.Name != "." && item.Name != "..")
                    {
                        isEmpty = false;
                        break;
                    }

                switch (isEmpty)
                {
                    case false when !force:
                        _logger.LogWarning("Bucket not empty and force=false: {BucketName}", bucketName);
                        client.Disconnect();
                        return false;
                    case false when force:
                        DeleteDirectoryRecursive(client, bucketPath);
                        break;
                }

                client.DeleteDirectory(bucketPath);

                var success = !client.Exists(bucketPath);

                client.Disconnect();

                if (success)
                    _logger.LogInformation("Bucket deleted: {BucketName}", bucketName);
                else
                    _logger.LogWarning("Bucket delete failed: {BucketName}", bucketName);

                return success;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            LogError(ex, "SFTP bucket delete error: {BucketName}", bucketName);
            return false;
        }
    }

    /// <summary>
    /// Recursively deletes a directory and all its contents
    /// </summary>
    private void DeleteDirectoryRecursive(SftpClient client, string directoryPath)
    {
        var files = client.ListDirectory(directoryPath);

        foreach (var file in files)
        {
            if (file.Name == "." || file.Name == "..")
                continue;

            if (file.IsDirectory)
                DeleteDirectoryRecursive(client, file.FullName);
            else
                client.DeleteFile(file.FullName);
        }

        if (directoryPath != _options.RootPath) client.DeleteDirectory(directoryPath);
    }
}
