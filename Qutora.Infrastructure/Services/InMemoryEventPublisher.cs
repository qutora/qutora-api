using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Qutora.Application.Interfaces;

namespace Qutora.Infrastructure.Services;

/// <summary>
/// Simple in-memory event publisher that queues events for background processing
/// </summary>
public class InMemoryEventPublisher(IServiceProvider serviceProvider, ILogger<InMemoryEventPublisher> logger)
    : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private static readonly ConcurrentQueue<(Type EventType, object EventData)> _eventQueue = new();

    public Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : class
    {
        _eventQueue.Enqueue((typeof(T), eventData));
        logger.LogInformation("Event published: {EventType}", typeof(T).Name);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the next event from the queue (used by background service)
    /// </summary>
    public static bool TryDequeue(out (Type EventType, object EventData) eventItem)
    {
        return _eventQueue.TryDequeue(out eventItem);
    }
} 