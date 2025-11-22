using Finbuckle.MultiTenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantIdentityApi.Domain.Entities;

namespace MultiTenantIdentityApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for ApplicationUser
/// Configures multi-tenant support, indexes, and constraints
/// </summary>
public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Table name
        builder.ToTable("Users");

        // Configure properties
        builder.Property(u => u.FirstName)
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .HasMaxLength(100);

        builder.Property(u => u.TenantId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(u => u.UpdatedAt)
            .IsRequired(false);

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Multi-tenant configuration
        // This adds query filters and ensures tenant isolation
        builder.IsMultiTenant();

        // Create composite index for tenant + email uniqueness
        // This ensures email uniqueness within a tenant (not globally)
        builder.HasIndex(u => new { u.TenantId, u.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName("IX_Users_Tenant_Email");

        // Create composite index for tenant + username uniqueness
        // This ensures username uniqueness within a tenant (not globally)
        builder.HasIndex(u => new { u.TenantId, u.NormalizedUserName })
            .IsUnique()
            .HasDatabaseName("IX_Users_Tenant_UserName");

        // Index on TenantId for query performance
        builder.HasIndex(u => u.TenantId)
            .HasDatabaseName("IX_Users_TenantId");

        // Index on IsActive for filtering active users
        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        // Composite index for common queries
        builder.HasIndex(u => new { u.TenantId, u.IsActive })
            .HasDatabaseName("IX_Users_Tenant_IsActive");
    }
}
