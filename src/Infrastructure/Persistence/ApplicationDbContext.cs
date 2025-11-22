using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Domain.Entities;

namespace MultiTenantIdentityApi.Infrastructure.Persistence;

/// <summary>
/// Main application database context with multi-tenant Identity support
/// </summary>
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
    /// Current tenant information
    /// </summary>
    public AppTenantInfo? CurrentTenant => _tenantAccessor.MultiTenantContext?.TenantInfo;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity tables with tenant isolation
        ConfigureIdentityTables(builder);

        // Configure multi-tenant query filters
        ConfigureMultiTenantFilters(builder);
    }

    private void ConfigureIdentityTables(ModelBuilder builder)
    {
        // Rename Identity tables (optional)
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");

            entity.Property(u => u.FirstName)
                .HasMaxLength(100);

            entity.Property(u => u.LastName)
                .HasMaxLength(100);

            entity.Property(u => u.TenantId)
                .HasMaxLength(64)
                .IsRequired();

            // Create composite index for tenant + email uniqueness
            entity.HasIndex(u => new { u.TenantId, u.NormalizedEmail })
                .IsUnique()
                .HasDatabaseName("IX_Users_Tenant_Email");

            // Create composite index for tenant + username uniqueness
            entity.HasIndex(u => new { u.TenantId, u.NormalizedUserName })
                .IsUnique()
                .HasDatabaseName("IX_Users_Tenant_UserName");
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles");

            entity.Property(r => r.TenantId)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(r => r.Description)
                .HasMaxLength(500);

            // Create composite index for tenant + role name uniqueness
            entity.HasIndex(r => new { r.TenantId, r.NormalizedName })
                .IsUnique()
                .HasDatabaseName("IX_Roles_Tenant_Name");
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });
    }

    private void ConfigureMultiTenantFilters(ModelBuilder builder)
    {
        // Finbuckle automatically adds query filters for IMultiTenant entities
        // This ensures data isolation between tenants

        builder.Entity<ApplicationUser>().IsMultiTenant();
        builder.Entity<ApplicationRole>().IsMultiTenant();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTenantIdOnEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetTenantIdOnEntities();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetTenantIdOnEntities()
    {
        var tenantId = CurrentTenant?.Id;

        if (string.IsNullOrEmpty(tenantId))
            return;

        foreach (var entry in ChangeTracker.Entries<IMultiTenant>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}
