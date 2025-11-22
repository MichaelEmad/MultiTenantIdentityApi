using Ardalis.Specification;
using System.Linq.Expressions;

namespace MultiTenantIdentityApi.Domain.Interfaces;

/// <summary>
/// Generic repository interface with Specification pattern support
/// </summary>
public interface IRepository<T> : IRepositoryBase<T> where T : class
{
    // Ardalis.Specification provides these methods automatically:
    // - Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default);
    // - Task<T?> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    // - Task<TResult?> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default);
    // - Task<T?> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default);
    // - Task<TResult?> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default);
    // - Task<List<T>> ListAsync(CancellationToken cancellationToken = default);
    // - Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    // - Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default);
    // - Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    // - Task<int> CountAsync(CancellationToken cancellationToken = default);
    // - Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    // - Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    // - Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    // - Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    // - Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    // - Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    // - Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    // - Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    // - Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Keep legacy methods for backward compatibility (can be removed later)
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
}
