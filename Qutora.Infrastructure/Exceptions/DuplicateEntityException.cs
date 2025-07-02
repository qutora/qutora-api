namespace Qutora.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a duplicate entity
/// </summary>
public class DuplicateEntityException : Exception
{
    public DuplicateEntityException(string message) : base(message)
    {
    }

    public DuplicateEntityException(string message, Exception innerException) : base(message, innerException)
    {
    }
}