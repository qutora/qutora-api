namespace Qutora.Application.Interfaces;

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to be processed by background jobs
    /// </summary>
    Task PublishAsync<T>(T eventData, CancellationToken cancellationToken = default) where T : class;
} 