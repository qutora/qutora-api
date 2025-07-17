using System.Linq.Expressions;

namespace Qutora.Application.Interfaces.Repositories;

/// <summary>
/// Generic repository interface that defines common operations for all entities
/// Compliant with Clean Architecture standards
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets all records
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets record by ID - returns null if not found
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds single record - returns null if not found
    /// </summary>
    Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds records matching condition
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds record
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple records
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates record (synchronous)
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Updates record (asynchronous) - for service compatibility
    /// </summary>
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes record
    /// </summary>
    void Delete(T entity);

    /// <summary>
    /// Removes record
    /// </summary>
    void Remove(T entity);

    /// <summary>
    /// Removes multiple records
    /// </summary>
    void RemoveRange(IEnumerable<T> entities);

    /// <summary>
    /// Returns queryable
    /// </summary>
    IQueryable<T> GetQueryable();

    /// <summary>
    /// Reloads entity
    /// </summary>
    Task ReloadEntityAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if record exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if record matching condition exists
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets record count
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of records matching condition
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}