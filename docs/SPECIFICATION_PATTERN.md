# Specification Pattern Implementation

This document describes the implementation of the **Ardalis.Specification** pattern in the MultiTenantIdentityApi project.

## Table of Contents

- [Overview](#overview)
- [Benefits](#benefits)
- [Architecture](#architecture)
- [Implementation](#implementation)
- [Available Specifications](#available-specifications)
- [Usage Examples](#usage-examples)
- [Creating New Specifications](#creating-new-specifications)
- [Best Practices](#best-practices)
- [Testing Specifications](#testing-specifications)
- [Migration Guide](#migration-guide)

## Overview

The Specification pattern is a domain-driven design pattern that encapsulates query logic into reusable, testable, and composable objects. We use the **Ardalis.Specification** library which provides a robust implementation with Entity Framework Core integration.

### What is a Specification?

A specification is an object that encapsulates query logic. Instead of writing LINQ queries directly in your services or repositories, you create specification classes that define:

- **Filtering**: WHERE clauses
- **Ordering**: ORDER BY clauses
- **Paging**: SKIP/TAKE operations
- **Includes**: Related entity loading
- **Projections**: SELECT transformations

### Why Use Specifications?

```csharp
// ❌ Before: Query logic scattered in services
var users = await _context.Users
    .Where(u => u.TenantId == tenantId && u.IsActive)
    .OrderBy(u => u.CreatedAt)
    .Skip(skip)
    .Take(take)
    .ToListAsync();

// ✅ After: Reusable, testable specification
var spec = new PaginatedUsersInTenantSpec(tenantId, skip, take);
var users = await _repository.ListAsync(spec);
```

## Benefits

### 1. **Reusability**
Query logic is defined once and reused across multiple services and controllers.

```csharp
// Use the same specification in multiple places
var spec = new ActiveUsersInTenantSpec(tenantId);
var users = await _userRepository.ListAsync(spec);
var count = await _userRepository.CountAsync(spec);
```

### 2. **Testability**
Specifications can be unit tested in isolation without a database.

```csharp
[Fact]
public void ActiveUsersInTenantSpec_FiltersCorrectly()
{
    var spec = new ActiveUsersInTenantSpec("tenant-1");
    var query = spec.Query;

    // Assert on the specification logic
    Assert.NotNull(query.WhereExpressions);
    Assert.Single(query.WhereExpressions);
}
```

### 3. **Maintainability**
Query changes happen in one place, reducing bugs and improving consistency.

### 4. **Type Safety**
Compile-time checking ensures queries are valid against your domain model.

### 5. **Composability**
Specifications can be combined and extended for complex queries.

### 6. **Separation of Concerns**
Query logic lives in the Domain layer, not scattered across Application/Infrastructure.

## Architecture

### Package Structure

```
MultiTenantIdentityApi.Domain
├── Ardalis.Specification (8.0.0)
└── Specifications/
    ├── UserSpecifications.cs
    └── TenantSpecifications.cs

MultiTenantIdentityApi.Infrastructure
├── Ardalis.Specification.EntityFrameworkCore (8.0.0)
└── Persistence/
    └── Repositories/
        └── Repository.cs
```

### Repository Pattern Integration

```csharp
// Domain Layer: Interface
public interface IRepository<T> : IRepositoryBase<T> where T : class
{
    // Inherits Ardalis methods:
    // - FirstOrDefaultAsync(ISpecification<T> spec)
    // - ListAsync(ISpecification<T> spec)
    // - CountAsync(ISpecification<T> spec)
    // - AnyAsync(ISpecification<T> spec)

    // Legacy methods for backward compatibility
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
}

// Infrastructure Layer: Implementation
public class Repository<T> : RepositoryBase<T>, IRepository<T> where T : class
{
    private readonly DbContext _dbContext;

    public Repository(DbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // Inherits all Ardalis specification methods
    // Implements legacy methods for backward compatibility
}
```

## Implementation

### Base Repository Setup

The base `Repository<T>` class extends `RepositoryBase<T>` from Ardalis.Specification:

**Location**: `src/Infrastructure/Persistence/Repositories/Repository.cs`

```csharp
public class Repository<T> : RepositoryBase<T>, IRepository<T> where T : class
{
    private readonly DbContext _dbContext;

    public Repository(DbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual async Task<T?> GetByIdAsync(string id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().FindAsync(
            new object[] { id },
            cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>().ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<T>()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    // Other legacy methods...
}
```

### Specification Classes

Specifications are placed in the **Domain layer** under `Specifications/` folder.

## Available Specifications

### User Specifications

**Location**: `src/Domain/Specifications/UserSpecifications.cs`

#### 1. UserByEmailSpec
Finds a user by exact email match (case-sensitive).

```csharp
var spec = new UserByEmailSpec("user@example.com");
var user = await _repository.FirstOrDefaultAsync(spec);
```

#### 2. UserByEmailIgnoreCaseSpec
Finds a user by email with case-insensitive matching.

```csharp
var spec = new UserByEmailIgnoreCaseSpec("USER@EXAMPLE.COM");
var user = await _repository.FirstOrDefaultAsync(spec);
```

#### 3. UserByUsernameSpec
Finds a user by username.

```csharp
var spec = new UserByUsernameSpec("john.doe");
var user = await _repository.FirstOrDefaultAsync(spec);
```

#### 4. ActiveUsersInTenantSpec
Returns all active users in a specific tenant, ordered by creation date.

```csharp
var spec = new ActiveUsersInTenantSpec("tenant-123");
var users = await _repository.ListAsync(spec);
```

#### 5. UsersInTenantSpec
Returns all users (active and inactive) in a tenant, ordered by creation date.

```csharp
var spec = new UsersInTenantSpec("tenant-123");
var users = await _repository.ListAsync(spec);
```

#### 6. PaginatedUsersInTenantSpec
Returns paginated users in a tenant.

```csharp
var spec = new PaginatedUsersInTenantSpec("tenant-123", skip: 0, take: 20);
var users = await _repository.ListAsync(spec);
```

#### 7. SearchUsersSpec
Searches users by email, first name, or last name within a tenant.

```csharp
var spec = new SearchUsersSpec("tenant-123", "john");
var users = await _repository.ListAsync(spec);
```

#### 8. CountActiveUsersInTenantSpec
Used for counting active users in a tenant.

```csharp
var spec = new CountActiveUsersInTenantSpec("tenant-123");
var count = await _repository.CountAsync(spec);
```

#### 9. UsersCreatedInRangeSpec
Returns users created within a date range, ordered by creation date (newest first).

```csharp
var spec = new UsersCreatedInRangeSpec(
    fromDate: new DateTime(2024, 1, 1),
    toDate: new DateTime(2024, 12, 31)
);
var users = await _repository.ListAsync(spec);
```

### Tenant Specifications

**Location**: `src/Domain/Specifications/TenantSpecifications.cs`

#### 1. TenantByIdentifierSpec
Finds a tenant by unique identifier.

```csharp
var spec = new TenantByIdentifierSpec("tenant-abc");
var tenant = await _repository.FirstOrDefaultAsync(spec);
```

#### 2. TenantByIdSpec
Finds a tenant by ID.

```csharp
var spec = new TenantByIdSpec("123");
var tenant = await _repository.FirstOrDefaultAsync(spec);
```

#### 3. ActiveTenantsSpec
Returns all active tenants, ordered by name.

```csharp
var spec = new ActiveTenantsSpec();
var tenants = await _repository.ListAsync(spec);
```

#### 4. AllTenantsSpec
Returns all tenants (active and inactive), ordered by name.

```csharp
var spec = new AllTenantsSpec();
var tenants = await _repository.ListAsync(spec);
```

#### 5. PaginatedTenantsSpec
Returns paginated tenants with optional active-only filtering.

```csharp
var spec = new PaginatedTenantsSpec(skip: 0, take: 10, activeOnly: true);
var tenants = await _repository.ListAsync(spec);
```

#### 6. SearchTenantsSpec
Searches tenants by name or identifier.

```csharp
var spec = new SearchTenantsSpec("acme");
var tenants = await _repository.ListAsync(spec);
```

#### 7. CountActiveTenantsSpec
Used for counting active tenants.

```csharp
var spec = new CountActiveTenantsSpec();
var count = await _repository.CountAsync(spec);
```

#### 8. TenantsCreatedInRangeSpec
Returns tenants created within a date range.

```csharp
var spec = new TenantsCreatedInRangeSpec(
    fromDate: new DateTime(2024, 1, 1),
    toDate: new DateTime(2024, 12, 31)
);
var tenants = await _repository.ListAsync(spec);
```

## Usage Examples

### Basic Query

```csharp
public class UserService
{
    private readonly IRepository<ApplicationUser> _userRepository;

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        var spec = new UserByEmailSpec(email);
        return await _userRepository.FirstOrDefaultAsync(spec);
    }
}
```

### Counting

```csharp
public async Task<int> GetActiveTenantCountAsync()
{
    var spec = new CountActiveTenantsSpec();
    return await _tenantRepository.CountAsync(spec);
}
```

### Checking Existence

```csharp
public async Task<bool> UserExistsAsync(string email)
{
    var spec = new UserByEmailSpec(email);
    return await _userRepository.AnyAsync(spec);
}
```

### Pagination

```csharp
public async Task<PaginatedResult<ApplicationUser>> GetUsersAsync(
    string tenantId,
    int page,
    int pageSize)
{
    var skip = (page - 1) * pageSize;

    var spec = new PaginatedUsersInTenantSpec(tenantId, skip, pageSize);
    var users = await _userRepository.ListAsync(spec);

    var countSpec = new UsersInTenantSpec(tenantId);
    var totalCount = await _userRepository.CountAsync(countSpec);

    return new PaginatedResult<ApplicationUser>
    {
        Items = users,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

### Search

```csharp
public async Task<IEnumerable<ApplicationUser>> SearchUsersAsync(
    string tenantId,
    string searchTerm)
{
    if (string.IsNullOrWhiteSpace(searchTerm))
    {
        var allSpec = new ActiveUsersInTenantSpec(tenantId);
        return await _userRepository.ListAsync(allSpec);
    }

    var searchSpec = new SearchUsersSpec(tenantId, searchTerm);
    return await _userRepository.ListAsync(searchSpec);
}
```

## Creating New Specifications

### Single Result Specification

For queries that return a single entity (e.g., by ID, email):

```csharp
/// <summary>
/// Specification for finding a user by phone number
/// </summary>
public sealed class UserByPhoneNumberSpec : Specification<ApplicationUser>,
    ISingleResultSpecification<ApplicationUser>
{
    public UserByPhoneNumberSpec(string phoneNumber)
    {
        Query.Where(u => u.PhoneNumber == phoneNumber);
    }
}
```

**Key points**:
- Implement `ISingleResultSpecification<T>` for single result queries
- Use `sealed` for better performance
- Add XML documentation

### List Specification

For queries that return multiple entities:

```csharp
/// <summary>
/// Specification for finding users by role
/// </summary>
public sealed class UsersByRoleSpec : Specification<ApplicationUser>
{
    public UsersByRoleSpec(string roleName)
    {
        Query
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName))
            .OrderBy(u => u.Email);
    }
}
```

### Complex Specification with Includes

```csharp
/// <summary>
/// Specification for getting user with roles and claims
/// </summary>
public sealed class UserWithRolesAndClaimsSpec : Specification<ApplicationUser>
{
    public UserWithRolesAndClaimsSpec(string userId)
    {
        Query
            .Where(u => u.Id == userId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserClaims);
    }
}
```

### Specification with Projections

```csharp
/// <summary>
/// Specification for getting user summary information
/// </summary>
public sealed class UserSummarySpec : Specification<ApplicationUser, UserSummaryDto>
{
    public UserSummarySpec(string tenantId)
    {
        Query
            .Where(u => u.TenantId == tenantId)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = $"{u.FirstName} {u.LastName}",
                IsActive = u.IsActive
            });
    }
}

// Usage
var spec = new UserSummarySpec(tenantId);
var summaries = await _repository.ListAsync(spec); // Returns UserSummaryDto[]
```

## Best Practices

### 1. **Naming Conventions**

```csharp
// ✅ Good: Descriptive, indicates what it returns
public class ActiveUsersInTenantSpec { }
public class UserByEmailSpec { }
public class PaginatedTenantsSpec { }

// ❌ Bad: Vague, unclear
public class UserSpec { }
public class GetDataSpec { }
```

### 2. **Keep Specifications Focused**

```csharp
// ✅ Good: Single responsibility
public class ActiveUsersSpec : Specification<ApplicationUser>
{
    public ActiveUsersSpec()
    {
        Query.Where(u => u.IsActive);
    }
}

// ❌ Bad: Too many parameters, doing too much
public class UserFilterSpec : Specification<ApplicationUser>
{
    public UserFilterSpec(
        string? tenantId,
        bool? isActive,
        string? email,
        DateTime? createdAfter,
        string? role,
        int? skip,
        int? take)
    {
        // Complex logic...
    }
}
```

### 3. **Use Sealed Classes**

```csharp
// ✅ Good: Sealed for performance
public sealed class UserByIdSpec : Specification<ApplicationUser> { }

// ❌ Less optimal: Not sealed (unless inheritance is intended)
public class UserByIdSpec : Specification<ApplicationUser> { }
```

### 4. **Validate Constructor Parameters**

```csharp
public sealed class UserByEmailSpec : Specification<ApplicationUser>
{
    public UserByEmailSpec(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        Query.Where(u => u.Email == email);
    }
}
```

### 5. **Document Your Specifications**

```csharp
/// <summary>
/// Specification for finding all active users in a tenant.
/// Returns users ordered by creation date (oldest first).
/// </summary>
/// <example>
/// <code>
/// var spec = new ActiveUsersInTenantSpec("tenant-123");
/// var users = await repository.ListAsync(spec);
/// </code>
/// </example>
public sealed class ActiveUsersInTenantSpec : Specification<ApplicationUser>
{
    // ...
}
```

### 6. **Don't Mix Concerns**

```csharp
// ✅ Good: Separate specifications
var filterSpec = new ActiveUsersInTenantSpec(tenantId);
var paginationSpec = new PaginatedUsersSpec(skip, take);

// ❌ Bad: Mixing filtering and pagination
var spec = new ActiveUsersPaginatedSpec(tenantId, skip, take);
```

### 7. **Prefer Composition Over Inheritance**

```csharp
// ✅ Good: Create separate, composable specifications
public sealed class ActiveUsersSpec : Specification<ApplicationUser>
{
    public ActiveUsersSpec()
    {
        Query.Where(u => u.IsActive);
    }
}

public sealed class UsersInTenantSpec : Specification<ApplicationUser>
{
    public UsersInTenantSpec(string tenantId)
    {
        Query.Where(u => u.TenantId == tenantId);
    }
}

// ❌ Avoid: Inheritance hierarchies
public abstract class BaseUserSpec : Specification<ApplicationUser> { }
public class ActiveUserSpec : BaseUserSpec { }
```

## Testing Specifications

### Unit Testing Example

```csharp
public class UserSpecificationTests
{
    [Fact]
    public void UserByEmailSpec_CreatesCorrectQuery()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var spec = new UserByEmailSpec(email);

        // Assert
        Assert.NotNull(spec.Query);
        Assert.NotEmpty(spec.Query.WhereExpressions);

        // Optionally test against in-memory data
        var users = GetTestUsers();
        var evaluator = new InMemorySpecificationEvaluator();
        var result = evaluator.Evaluate(users, spec);

        Assert.Single(result);
        Assert.Equal(email, result.First().Email);
    }

    [Fact]
    public void ActiveUsersInTenantSpec_FiltersCorrectly()
    {
        // Arrange
        var tenantId = "tenant-1";
        var spec = new ActiveUsersInTenantSpec(tenantId);

        // Act
        var users = GetTestUsers();
        var evaluator = new InMemorySpecificationEvaluator();
        var result = evaluator.Evaluate(users, spec).ToList();

        // Assert
        Assert.All(result, u => Assert.Equal(tenantId, u.TenantId));
        Assert.All(result, u => Assert.True(u.IsActive));
    }

    private List<ApplicationUser> GetTestUsers()
    {
        return new List<ApplicationUser>
        {
            new() { Id = "1", Email = "test@example.com", TenantId = "tenant-1", IsActive = true },
            new() { Id = "2", Email = "user@example.com", TenantId = "tenant-1", IsActive = false },
            new() { Id = "3", Email = "admin@example.com", TenantId = "tenant-2", IsActive = true }
        };
    }
}
```

### Integration Testing Example

```csharp
public class UserRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly IRepository<ApplicationUser> _repository;

    [Fact]
    public async Task UserByEmailSpec_ReturnsCorrectUser()
    {
        // Arrange
        var email = "integration@test.com";
        await SeedUserAsync(email);

        var spec = new UserByEmailSpec(email);

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }
}
```

## Migration Guide

### Migrating from Direct LINQ Queries

#### Before: Direct Repository Queries

```csharp
public class AuthService
{
    private readonly ApplicationDbContext _context;

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Where(u => u.Email == email)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ApplicationUser>> GetActiveUsersAsync(string tenantId)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
    }
}
```

#### After: Using Specifications

```csharp
public class AuthService
{
    private readonly IRepository<ApplicationUser> _userRepository;

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        var spec = new UserByEmailSpec(email);
        return await _userRepository.FirstOrDefaultAsync(spec);
    }

    public async Task<List<ApplicationUser>> GetActiveUsersAsync(string tenantId)
    {
        var spec = new ActiveUsersInTenantSpec(tenantId);
        return await _userRepository.ListAsync(spec);
    }
}
```

### Migration Checklist

- [ ] Identify repeated query patterns in your services
- [ ] Create specifications for common queries
- [ ] Update service dependencies to use `IRepository<T>` instead of `DbContext`
- [ ] Replace direct LINQ queries with specification usage
- [ ] Add unit tests for your specifications
- [ ] Remove unused `using` statements for Entity Framework
- [ ] Update integration tests to use specifications

## Common Patterns

### Pattern 1: List with Count

```csharp
public async Task<PagedResult<Tenant>> GetTenantsPagedAsync(int page, int pageSize)
{
    var skip = (page - 1) * pageSize;

    var listSpec = new PaginatedTenantsSpec(skip, pageSize, activeOnly: true);
    var countSpec = new CountActiveTenantsSpec();

    var tenants = await _repository.ListAsync(listSpec);
    var total = await _repository.CountAsync(countSpec);

    return new PagedResult<Tenant>
    {
        Items = tenants,
        Total = total,
        Page = page,
        PageSize = pageSize
    };
}
```

### Pattern 2: Check Existence Before Operation

```csharp
public async Task<Result> DeleteUserAsync(string email)
{
    var spec = new UserByEmailSpec(email);

    if (!await _repository.AnyAsync(spec))
        return Result.Failure("User not found");

    var user = await _repository.FirstOrDefaultAsync(spec);
    await _repository.DeleteAsync(user);

    return Result.Success();
}
```

### Pattern 3: Conditional Specifications

```csharp
public async Task<List<User>> SearchUsersAsync(string tenantId, string? searchTerm)
{
    ISpecification<ApplicationUser> spec = string.IsNullOrWhiteSpace(searchTerm)
        ? new ActiveUsersInTenantSpec(tenantId)
        : new SearchUsersSpec(tenantId, searchTerm);

    return await _repository.ListAsync(spec);
}
```

## Performance Considerations

### 1. **Use AsNoTracking for Read-Only Queries**

```csharp
public sealed class ActiveUsersReadOnlySpec : Specification<ApplicationUser>
{
    public ActiveUsersReadOnlySpec()
    {
        Query
            .Where(u => u.IsActive)
            .AsNoTracking(); // Improves performance for read-only scenarios
    }
}
```

### 2. **Optimize Includes**

```csharp
// ✅ Good: Only include what you need
public sealed class UserWithRolesSpec : Specification<ApplicationUser>
{
    public UserWithRolesSpec(string userId)
    {
        Query
            .Where(u => u.Id == userId)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);
    }
}

// ❌ Bad: Excessive includes
public sealed class UserWithEverythingSpec : Specification<ApplicationUser>
{
    public UserWithEverythingSpec(string userId)
    {
        Query
            .Where(u => u.Id == userId)
            .Include(u => u.UserRoles)
            .Include(u => u.UserClaims)
            .Include(u => u.UserLogins)
            .Include(u => u.UserTokens); // May not need all of these
    }
}
```

### 3. **Use Projections for Large Result Sets**

```csharp
public sealed class UserEmailsSpec : Specification<ApplicationUser, string>
{
    public UserEmailsSpec(string tenantId)
    {
        Query
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Email!); // Only select what you need
    }
}
```

## Troubleshooting

### Issue: Specification Not Filtering Correctly

**Symptom**: Query returns all records instead of filtered subset.

**Solution**: Check that you're using the correct query builder:

```csharp
// ✅ Correct
Query.Where(u => u.IsActive);

// ❌ Wrong: Doesn't modify the specification
var query = Query;
query.Where(u => u.IsActive); // This doesn't work
```

### Issue: Include Not Working

**Symptom**: Navigation properties are null.

**Solution**: Ensure you're using `Include` and `ThenInclude` correctly:

```csharp
Query
    .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role); // Use ThenInclude for nested
```

### Issue: Cannot Convert Specification to Expression

**Symptom**: Compile error when trying to use specification.

**Solution**: Use repository methods, not direct LINQ:

```csharp
// ✅ Correct
var spec = new UserByEmailSpec(email);
var user = await _repository.FirstOrDefaultAsync(spec);

// ❌ Wrong
var spec = new UserByEmailSpec(email);
var user = await _context.Users.Where(spec).FirstOrDefaultAsync(); // Won't work
```

## Resources

- **Ardalis.Specification Documentation**: https://github.com/ardalis/Specification
- **Specification Pattern**: https://deviq.com/design-patterns/specification-pattern
- **Domain-Driven Design**: https://www.domainlanguage.com/ddd/

## Summary

The Specification pattern provides a clean, testable way to encapsulate query logic:

- ✅ **Reusable**: Write once, use everywhere
- ✅ **Testable**: Unit test without database
- ✅ **Maintainable**: Changes in one place
- ✅ **Type-safe**: Compile-time checking
- ✅ **Clean**: Separation of concerns

By following the patterns and practices outlined in this document, you'll create a maintainable and testable data access layer.
