using Ardalis.Specification;
using MultiTenantIdentityApi.Domain.Entities;

namespace MultiTenantIdentityApi.Domain.Specifications;

/// <summary>
/// Specification for finding a user by email
/// </summary>
public sealed class UserByEmailSpec : Specification<ApplicationUser>, ISingleResultSpecification<ApplicationUser>
{
    public UserByEmailSpec(string email)
    {
        Query.Where(u => u.Email == email);
    }
}

/// <summary>
/// Specification for finding a user by email (case-insensitive)
/// </summary>
public sealed class UserByEmailIgnoreCaseSpec : Specification<ApplicationUser>, ISingleResultSpecification<ApplicationUser>
{
    public UserByEmailIgnoreCaseSpec(string email)
    {
        Query.Where(u => u.NormalizedEmail == email.ToUpper());
    }
}

/// <summary>
/// Specification for finding a user by username
/// </summary>
public sealed class UserByUsernameSpec : Specification<ApplicationUser>, ISingleResultSpecification<ApplicationUser>
{
    public UserByUsernameSpec(string username)
    {
        Query.Where(u => u.UserName == username);
    }
}

/// <summary>
/// Specification for finding active users in a tenant
/// </summary>
public sealed class ActiveUsersInTenantSpec : Specification<ApplicationUser>
{
    public ActiveUsersInTenantSpec(string tenantId)
    {
        Query
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.CreatedAt);
    }
}

/// <summary>
/// Specification for finding all users in a tenant
/// </summary>
public sealed class UsersInTenantSpec : Specification<ApplicationUser>
{
    public UsersInTenantSpec(string tenantId)
    {
        Query
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.CreatedAt);
    }
}

/// <summary>
/// Specification for paginated users in a tenant
/// </summary>
public sealed class PaginatedUsersInTenantSpec : Specification<ApplicationUser>
{
    public PaginatedUsersInTenantSpec(string tenantId, int skip, int take)
    {
        Query
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.CreatedAt)
            .Skip(skip)
            .Take(take);
    }
}

/// <summary>
/// Specification for searching users by name or email
/// </summary>
public sealed class SearchUsersSpec : Specification<ApplicationUser>
{
    public SearchUsersSpec(string tenantId, string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();

        Query
            .Where(u => u.TenantId == tenantId &&
                       (u.Email!.ToLower().Contains(lowerSearchTerm) ||
                        u.FirstName!.ToLower().Contains(lowerSearchTerm) ||
                        u.LastName!.ToLower().Contains(lowerSearchTerm) ||
                        u.UserName!.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(u => u.Email);
    }
}

/// <summary>
/// Specification for counting active users in a tenant
/// </summary>
public sealed class CountActiveUsersInTenantSpec : Specification<ApplicationUser>
{
    public CountActiveUsersInTenantSpec(string tenantId)
    {
        Query.Where(u => u.TenantId == tenantId && u.IsActive);
    }
}

/// <summary>
/// Specification for users created within a date range
/// </summary>
public sealed class UsersCreatedInRangeSpec : Specification<ApplicationUser>
{
    public UsersCreatedInRangeSpec(string tenantId, DateTime fromDate, DateTime toDate)
    {
        Query
            .Where(u => u.TenantId == tenantId &&
                       u.CreatedAt >= fromDate &&
                       u.CreatedAt <= toDate)
            .OrderByDescending(u => u.CreatedAt);
    }
}
