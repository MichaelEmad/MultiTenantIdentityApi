using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Domain.Entities;
using System.Reflection;

namespace MultiTenantIdentityApi.Infrastructure.Persistence;

/// <summary>
/// Main application database context with multi-tenant Identity support
/// Implements shared database multi-tenancy approach using Finbuckle.MultiTenant
/// </summary>
/// <remarks>
/// This DbContext uses the shared database approach where:
/// - All tenants share the same database
/// - Data isolation is achieved through TenantId filtering
/// - Entities implementing IMultiTenant are automatically filtered by current tenant
/// - Query filters ensure tenant data isolation at the EF Core level
/// </remarks>
public class ApplicationDbContext : MultiTenantIdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;

    public ApplicationDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        DbContextOptions<ApplicationDbContext> options)
        : base(tenantAccessor, options)
    {
        _tenantAccessor = tenantAccessor;
    }

    /// <summary>
    /// Gets the current tenant information from the multi-tenant context
    /// </summary>
    public AppTenantInfo? CurrentTenant => _tenantAccessor.MultiTenantContext?.TenantInfo;

    /// <summary>
    /// Configures the database schema and entity relationships
    /// </summary>
    /// <param name="builder">Model builder for EF Core configuration</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Call base to configure Identity tables
        // This sets up all ASP.NET Core Identity entities (Users, Roles, Claims, etc.)
        base.OnModelCreating(builder);

        // Apply all entity configurations from this assembly
        // This will automatically discover and apply all IEntityTypeConfiguration<T> classes
        // located in the Configurations folder
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure multi-tenant query filters for entities marked with [MultiTenant] attribute
        // Note: Entities using .IsMultiTenant() in their configuration are already handled
        // This is here for any entities that might use the attribute instead
        builder.ConfigureMultiTenant();
    }

    /// <summary>
    /// Saves all changes made in this context to the database
    /// Automatically sets TenantId on new multi-tenant entities
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether AcceptAllChanges is called after the changes have been sent successfully to the database
    /// </param>
    /// <returns>The number of state entries written to the database</returns>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTenantIdOnEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the database
    /// Automatically sets TenantId on new multi-tenant entities
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether AcceptAllChanges is called after the changes have been sent successfully to the database
    /// </param>
    /// <param name="cancellationToken">
    /// A CancellationToken to observe while waiting for the task to complete
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous save operation.
    /// The task result contains the number of state entries written to the database
    /// </returns>
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetTenantIdOnEntities();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Sets the TenantId property on all new entities implementing IMultiTenant
    /// This ensures that entities are always associated with the current tenant
    /// </summary>
    /// <remarks>
    /// This is called automatically before SaveChanges/SaveChangesAsync
    /// It only affects entities in the Added state (new entities)
    /// Existing entities retain their original TenantId to prevent data leakage
    /// </remarks>
    private void SetTenantIdOnEntities()
    {
        var tenantId = CurrentTenant?.Id;

        // If no tenant context is available, skip setting TenantId
        // This might happen during:
        // - Database migrations
        // - Seeding data
        // - Background jobs without tenant context
        if (string.IsNullOrEmpty(tenantId))
            return;

        // Find all entities implementing IMultiTenant that are being added
        var entries = ChangeTracker.Entries<IMultiTenant>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            // Set the TenantId to the current tenant
            // This ensures data is saved to the correct tenant
            entry.Entity.TenantId = tenantId;
        }
    }
}
