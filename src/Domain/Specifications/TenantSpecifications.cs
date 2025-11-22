using Ardalis.Specification;
using MultiTenantIdentityApi.Domain.Entities;

namespace MultiTenantIdentityApi.Domain.Specifications;

/// <summary>
/// Specification for finding a tenant by identifier
/// </summary>
public sealed class TenantByIdentifierSpec : Specification<AppTenantInfo>, ISingleResultSpecification<AppTenantInfo>
{
    public TenantByIdentifierSpec(string identifier)
    {
        Query.Where(t => t.Identifier == identifier);
    }
}

/// <summary>
/// Specification for finding a tenant by ID
/// </summary>
public sealed class TenantByIdSpec : Specification<AppTenantInfo>, ISingleResultSpecification<AppTenantInfo>
{
    public TenantByIdSpec(string id)
    {
        Query.Where(t => t.Id == id);
    }
}

/// <summary>
/// Specification for finding all active tenants
/// </summary>
public sealed class ActiveTenantsSpec : Specification<AppTenantInfo>
{
    public ActiveTenantsSpec()
    {
        Query
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name);
    }
}

/// <summary>
/// Specification for finding all tenants (active and inactive)
/// </summary>
public sealed class AllTenantsSpec : Specification<AppTenantInfo>
{
    public AllTenantsSpec()
    {
        Query.OrderBy(t => t.Name);
    }
}

/// <summary>
/// Specification for paginated tenants
/// </summary>
public sealed class PaginatedTenantsSpec : Specification<AppTenantInfo>
{
    public PaginatedTenantsSpec(int skip, int take, bool activeOnly = false)
    {
        if (activeOnly)
        {
            Query.Where(t => t.IsActive);
        }

        Query
            .OrderBy(t => t.Name)
            .Skip(skip)
            .Take(take);
    }
}

/// <summary>
/// Specification for searching tenants by name or identifier
/// </summary>
public sealed class SearchTenantsSpec : Specification<AppTenantInfo>
{
    public SearchTenantsSpec(string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        Query
            .Where(t => t.Name!.ToLower().Contains(lowerSearchTerm) ||
                       t.Identifier!.ToLower().Contains(lowerSearchTerm))
            .OrderBy(t => t.Name);
    }
}

/// <summary>
/// Specification for counting active tenants
/// </summary>
public sealed class CountActiveTenantsSpec : Specification<AppTenantInfo>
{
    public CountActiveTenantsSpec()
    {
        Query.Where(t => t.IsActive);
    }
}

/// <summary>
/// Specification for tenants created within a date range
/// </summary>
public sealed class TenantsCreatedInRangeSpec : Specification<AppTenantInfo>
{
    public TenantsCreatedInRangeSpec(DateTime fromDate, DateTime toDate)
    {
        Query
            .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
            .OrderByDescending(t => t.CreatedAt);
    }
}
