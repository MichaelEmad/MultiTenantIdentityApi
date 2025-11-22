using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Domain.Interfaces;
using System.Linq.Expressions;

namespace MultiTenantIdentityApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base repository implementation with Specification pattern support
/// </summary>
public class Repository<T> : RepositoryBase<T>, IRepository<T> where T : class
{
    private readonly DbContext _dbContext;

    public Repository(DbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // Legacy methods for backward compatibility
    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().Where(predicate).ToListAsync(cancellationToken);
    }
}
