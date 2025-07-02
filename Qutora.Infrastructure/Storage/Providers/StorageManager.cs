using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qutora.Domain.Entities;
using Qutora.Infrastructure.Interfaces.Repositories;
using Qutora.Infrastructure.Interfaces.Storage;
using Qutora.Infrastructure.Security;
using Qutora.Infrastructure.Storage.Exceptions;
using Qutora.Infrastructure.Storage.Models;
using Qutora.Infrastructure.Storage.Registry;
using Qutora.Shared.DTOs;

namespace Qutora.Infrastructure.Storage.Providers;

/// <summary>
/// Class that manages storage providers.
/// </summary>
public class StorageManager : IStorageManager
{
    private Dictionary<string, IStorageProvider> _providers = new();
    private string _defaultProviderId = "local";
    private readonly ILoggerFactory _loggerFactory;
    private readonly StorageProviderFactory _providerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StorageManager> _logger;
    private readonly ISensitiveDataProtector _dataProtector;
    private bool _isInitialized = false;
    private readonly object _lock = new();
    private bool _loggingEnabled = true;
    private Dictionary<string, DateTime> _providerLastModifiedTimes = new();

    /// <summary>
    /// Creates StorageManager class using database.
    /// </summary>
    public StorageManager(
        ILoggerFactory loggerFactory,
        StorageProviderFactory providerFactory,
        IServiceProvider serviceProvider,
        ISensitiveDataProtector dataProtector)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _dataProtector = dataProtector ?? throw new ArgumentNullException(nameof(dataProtector));
        _logger = loggerFactory.CreateLogger<StorageManager>();
    }

    /// <summary>
    /// Determines sensitive config keys based on provider type
    /// </summary>
    private string[] GetSensitiveConfigKeys(string providerType)
    {
        return providerType.ToLowerInvariant() switch
        {
            "minio" => ["accessKey", "secretKey"],
            "ftp" => ["password"],
            "sftp" => ["password", "privateKey", "privateKeyPassphrase"],
            _ => []
        };
    }

    /// <summary>
    /// Checks if there are changes in the provider list
    /// </summary>
    private bool CheckForChanges(List<StorageProvider> currentProviders)
    {
        if (_providerLastModifiedTimes.Count == 0)
            return true;

        if (_providerLastModifiedTimes.Count != currentProviders.Count)
            return true;

        foreach (var provider in currentProviders)
        {
            var providerId = provider.Id.ToString();
            if (!_providerLastModifiedTimes.TryGetValue(providerId, out var lastModified) ||
                lastModified < (provider.UpdatedAt ?? provider.CreatedAt))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Loads providers from database
    /// </summary>
    private async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        lock (_lock)
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IStorageProviderRepository>();

            var dbProviders = (await repository.GetAllActiveAsync()).ToList();

            var hasChanges = CheckForChanges(dbProviders);

            if (_loggingEnabled || hasChanges)
                _logger.LogInformation("Loaded {Count} storage providers from database", dbProviders.Count);

            var defaultProvider = dbProviders.FirstOrDefault(p => p is { IsDefault: true, IsActive: true });
            if (defaultProvider != null)
            {
                _defaultProviderId = defaultProvider.Id.ToString();

                if (_loggingEnabled || hasChanges)
                    _logger.LogInformation("Default storage provider set to: {ProviderName} (ID: {ProviderId})",
                        defaultProvider.Name, _defaultProviderId);
            }

            foreach (var provider in dbProviders)
                try
                {
                    var providerId = provider.Id.ToString();
                    IStorageProvider storageProvider;

                    var configJson =
                        _dataProtector.UnprotectSensitiveConfigJson(provider.ConfigJson, provider.ProviderType);

                    var config = ProviderConfigFactory.FromJson(provider.ProviderType, configJson);

                    config.ProviderId = providerId;

                    var adapter = _providerFactory.Create(config);

                    var capabilityCache = _serviceProvider.GetRequiredService<IStorageCapabilityCache>();

                    storageProvider = new StorageProviderWrapper(adapter, config.ProviderType, capabilityCache);

                    _providers[providerId] = storageProvider;

                    _providerLastModifiedTimes[providerId] = provider.UpdatedAt ?? provider.CreatedAt;

                    if (_loggingEnabled || hasChanges)
                        _logger.LogInformation(
                            "Initialized storage provider: {ProviderName} (ID: {ProviderId}, Type: {ProviderType})",
                            provider.Name, providerId, provider.ProviderType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing storage provider: {ProviderName} (ID: {ProviderId})",
                        provider.Name, provider.Id);
                }

            _loggingEnabled = false;

            if (_providers.Count == 0)
            {
                var localProviderId = "local";

                var config = new FileSystemProviderConfig
                {
                    ProviderId = localProviderId,
                    RootPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "local"),
                    CreateDirectoryIfNotExists = true
                };

                var adapter = _providerFactory.Create(config);
                var capabilityCache = _serviceProvider.GetRequiredService<IStorageCapabilityCache>();
                var fallbackProvider = new StorageProviderWrapper(adapter, "filesystem", capabilityCache);

                _providers[localProviderId] = fallbackProvider;
                _defaultProviderId = localProviderId;

                _logger.LogWarning(
                    "No active storage providers found in database. Added fallback local filesystem provider.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading storage providers from database");

            var localProviderId = "local";

            var config = new FileSystemProviderConfig
            {
                ProviderId = localProviderId,
                RootPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "local"),
                CreateDirectoryIfNotExists = true
            };

            var adapter = _providerFactory.Create(config);
            var capabilityCache = _serviceProvider.GetRequiredService<IStorageCapabilityCache>();
            var fallbackProvider = new StorageProviderWrapper(adapter, "filesystem", capabilityCache);

            _providers[localProviderId] = fallbackProvider;
            _defaultProviderId = localProviderId;

            _logger.LogWarning("Added fallback local filesystem provider due to initialization error");
        }
    }

    /// <summary>
    /// Reloads providers from database
    /// </summary>
    public async Task ReloadProvidersAsync()
    {
        _logger.LogInformation("Reloading storage providers from database");

        lock (_lock)
        {
            _isInitialized = false;
            _providers.Clear();
        }

        await InitializeAsync();
    }

    /// <summary>
    /// Returns provider with specified ID.
    /// Only active providers are returned.
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <returns>Storage provider</returns>
    public async Task<IStorageProvider> GetProviderAsync(string id)
    {
        await InitializeAsync();

        if (_providers.TryGetValue(id, out var providerFromDict))
        {
            // Additional security check: Verify if the provider is still active
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IStorageProviderRepository>();
            
            if (Guid.TryParse(id, out var providerId))
            {
                var dbProvider = await repository.GetByIdAsync(providerId);
                if (dbProvider == null || !dbProvider.IsActive)
                {
                    _logger.LogWarning("Attempt to access inactive storage provider: {ProviderId}", id);
                    throw new InvalidOperationException($"Storage provider {id} is not active or does not exist");
                }
            }
            
            return providerFromDict;
        }

        throw new ProviderNotFoundException(id);
    }

    /// <summary>
    /// Returns the default provider.
    /// </summary>
    /// <returns>Default storage provider</returns>
    public async Task<IStorageProvider> GetDefaultProviderAsync()
    {
        await InitializeAsync();

        if (_providers.TryGetValue(_defaultProviderId, out var provider))
            return provider;

        return _providers.Values.First();
    }

    /// <summary>
    /// Returns all registered provider IDs.
    /// </summary>
    /// <returns>List of provider IDs</returns>
    public async Task<IEnumerable<string>> GetAvailableProviderNamesAsync()
    {
        await InitializeAsync();
        return _providers.Keys;
    }

    /// <summary>
    /// Add or update provider
    /// </summary>
    public async Task AddOrUpdateProviderAsync(StorageProvider providerEntity)
    {
        if (providerEntity == null)
            throw new ArgumentNullException(nameof(providerEntity));

        _logger.LogInformation("Adding or updating storage provider: {ProviderName} (ID: {ProviderId})",
            providerEntity.Name, providerEntity.Id);

        try
        {
            var providerId = providerEntity.Id.ToString();

            var configJson =
                _dataProtector.UnprotectSensitiveConfigJson(providerEntity.ConfigJson, providerEntity.ProviderType);

            var config = ProviderConfigFactory.FromJson(providerEntity.ProviderType, configJson);
            config.ProviderId = providerId;
            var adapter = _providerFactory.Create(config);

            var capabilityCache = _serviceProvider.GetRequiredService<IStorageCapabilityCache>();
            var storageProvider = new StorageProviderWrapper(adapter, providerEntity.ProviderType, capabilityCache);

            _providers[providerId] = storageProvider;

            _providerLastModifiedTimes[providerId] = providerEntity.UpdatedAt ?? providerEntity.CreatedAt;

            if (providerEntity.IsDefault)
            {
                _defaultProviderId = providerId;
                _logger.LogInformation("Default storage provider set to: {ProviderName} (ID: {ProviderId})",
                    providerEntity.Name, _defaultProviderId);
            }

            _logger.LogInformation("Storage provider added/updated: {ProviderName} (ID: {ProviderId})",
                providerEntity.Name, providerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding/updating storage provider: {ProviderName} (ID: {ProviderId})",
                providerEntity.Name, providerEntity.Id);
            throw;
        }
    }

    /// <summary>
    /// Removes provider
    /// </summary>
    public async Task RemoveProviderAsync(string providerId)
    {
        await InitializeAsync();

        if (_providers.Remove(providerId))
        {
            _logger.LogInformation("Removed storage provider with ID: {ProviderId}", providerId);

            if (providerId == _defaultProviderId && _providers.Count > 0)
            {
                _defaultProviderId = _providers.Keys.First();
                _logger.LogInformation("Default storage provider changed to ID: {ProviderId} after removal",
                    _defaultProviderId);
            }
        }
    }

    /// <summary>
    /// Removes a specific provider from cache (for status changes)
    /// </summary>
    public async Task RemoveProviderFromCacheAsync(string providerId)
    {
        lock (_lock)
        {
            if (_providers.Remove(providerId))
            {
                _logger.LogInformation("Removed storage provider from cache: {ProviderId}", providerId);
            }
            
            if (_providerLastModifiedTimes.ContainsKey(providerId))
            {
                _providerLastModifiedTimes.Remove(providerId);
            }
        }
    }

    /// <summary>
    /// Tests provider connection
    /// </summary>
    /// <param name="providerType">Provider type</param>
    /// <param name="configJson">Provider configuration</param>
    /// <returns>Test result</returns>
    public async Task<(bool success, string message)> TestProviderConnectionAsync(string providerType,
        string configJson)
    {
        try
        {
            var configJsonDecrypted = _dataProtector.UnprotectSensitiveConfigJson(configJson, providerType);

            var config = ProviderConfigFactory.FromJson(providerType, configJsonDecrypted);

            if (config.ProviderType != providerType.ToLowerInvariant())
                return (false,
                    $"Config object ({config.ProviderType}) and specified provider type ({providerType}) do not match.");

            config.ProviderId = $"test-{Guid.NewGuid():N}";

            var provider = _providerFactory.Create(config);

            return await provider.TestConnectionAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing provider connection. Type: {ProviderType}", providerType);
            return (false, $"Connection test error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets bucket list from provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <returns>Bucket list</returns>
    public async Task<IEnumerable<BucketInfoDto>> ListBucketsAsync(string providerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
            throw new ArgumentNullException(nameof(providerId));

        await InitializeAsync();

        var provider = await GetProviderAsync(providerId);

        if (provider is IStorageProviderAdapter adapter)
        {
            var cacheKey = $"provider_{adapter.ProviderId}";

            try
            {
                return await adapter.ListBucketsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing buckets for provider {ProviderId}", providerId);
                throw;
            }
        }

        throw new InvalidOperationException($"Provider {providerId} is not a valid storage provider adapter");
    }

    /// <summary>
    /// Creates bucket on provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="bucketName">Bucket name</param>
    /// <returns>Operation result</returns>
    public async Task<bool> CreateBucketAsync(string providerId, string bucketName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
            throw new ArgumentNullException(nameof(providerId));

        if (string.IsNullOrEmpty(bucketName))
            throw new ArgumentNullException(nameof(bucketName));

        await InitializeAsync();

        var provider = await GetProviderAsync(providerId);

        if (provider is IStorageProviderAdapter adapter)
            try
            {
                return await adapter.CreateBucketAsync(bucketName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bucket {BucketName} for provider {ProviderId}", bucketName,
                    providerId);
                throw;
            }

        throw new InvalidOperationException($"Provider {providerId} is not a valid storage provider adapter");
    }

    /// <summary>
    /// Removes bucket from provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="bucketName">Bucket name</param>
    /// <param name="force">Force delete non-empty bucket</param>
    /// <returns>Operation result</returns>
    public async Task<bool> RemoveBucketAsync(string providerId, string bucketName, bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
            throw new ArgumentNullException(nameof(providerId));

        if (string.IsNullOrEmpty(bucketName))
            throw new ArgumentNullException(nameof(bucketName));

        await InitializeAsync();

        var provider = await GetProviderAsync(providerId);

        if (provider is IStorageProviderAdapter adapter)
            try
            {
                return await adapter.RemoveBucketAsync(bucketName, force, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing bucket {BucketName} for provider {ProviderId}", bucketName,
                    providerId);
                throw;
            }

        throw new InvalidOperationException($"Provider {providerId} is not a valid storage provider adapter");
    }

    /// <summary>
    /// Checks bucket on provider
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="bucketName">Bucket name</param>
    /// <returns>True if bucket exists</returns>
    public async Task<bool> BucketExistsAsync(string providerId, string bucketName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
            throw new ArgumentNullException(nameof(providerId));

        if (string.IsNullOrEmpty(bucketName))
            throw new ArgumentNullException(nameof(bucketName));

        await InitializeAsync();

        var provider = await GetProviderAsync(providerId);

        if (provider is IStorageProviderAdapter adapter)
            try
            {
                return await adapter.BucketExistsAsync(bucketName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if bucket {BucketName} exists for provider {ProviderId}",
                    bucketName, providerId);
                throw;
            }

        throw new InvalidOperationException($"Provider {providerId} is not a valid storage provider adapter");
    }
}
