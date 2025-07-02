namespace Qutora.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when attempting to operate on a deleted entity
/// </summary>
public class EntityDeletedException : Exception
{
    public EntityDeletedException(string message) : base(message)
    {
    }

    public EntityDeletedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}