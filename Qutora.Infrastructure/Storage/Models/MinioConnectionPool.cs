using Microsoft.Extensions.Logging;
using Minio;
using System.Collections.Concurrent;

namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// MinIO client connection pool.
/// Manages MinIO connections for high performance.
/// </summary>
public class MinioConnectionPool : IDisposable
{
    private readonly ConcurrentBag<IMinioClient> _clients = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly MinioProviderOptions _options;
    private readonly ILogger _logger;
    private readonly int _maxConnections;
    private readonly IHttpClientFactory _httpClientFactory;
    private bool _disposed = false;

    /// <summary>
    /// Creates MinioConnectionPool class using HttpClientFactory.
    /// </summary>
    /// <param name="options">MinIO connection options</param>
    /// <param name="logger">Logger object</param>
    /// <param name="httpClientFactory">HTTP client factory</param>
    /// <param name="maxConnections">Maximum number of connections, default 10</param>
    public MinioConnectionPool(MinioProviderOptions options, ILogger logger, IHttpClientFactory httpClientFactory,
        int maxConnections = 10)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _maxConnections = maxConnections > 0 ? maxConnections : 10;
        _semaphore = new SemaphoreSlim(_maxConnections, _maxConnections);

        for (var i = 0; i < _maxConnections / 2; i++) _clients.Add(CreateClientWithHttpFactory());

        _logger.LogInformation(
            "MinIO connection pool created with HttpClientFactory. Initial connection count: {Count}, Maximum: {Max}",
            _maxConnections / 2, _maxConnections);
    }

    /// <summary>
    /// Creates a new MinIO client using HttpClientFactory.
    /// </summary>
    private IMinioClient CreateClientWithHttpFactory()
    {
        return new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSSL)
            .WithHttpClient(_httpClientFactory.CreateClient("minio"))
            .Build();
    }

    /// <summary>
    /// Gets a MinIO client from the pool. Creates a new client if no available client exists and pool is not full.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation</param>
    /// <returns>MinIO client</returns>
    public async Task<IMinioClient> GetClientAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _semaphore.WaitAsync(cancellationToken);

        if (_clients.TryTake(out var client)) return client;

        return CreateClientWithHttpFactory();
    }

    /// <summary>
    /// Returns MinIO client back to the pool.
    /// </summary>
    /// <param name="client">Client to be returned</param>
    public void ReturnClient(IMinioClient client)
    {
        if (client == null || _disposed)
            return;

        _clients.Add(client);
        _semaphore.Release();
    }

    /// <summary>
    /// Clears all clients in the pool.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        while (_clients.TryTake(out var client))
            try
            {
                (client as IDisposable)?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred while disposing MinIO client");
            }

        _semaphore.Dispose();
        _logger.LogInformation("MinIO connection pool closed");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MinioConnectionPool));
    }
}
