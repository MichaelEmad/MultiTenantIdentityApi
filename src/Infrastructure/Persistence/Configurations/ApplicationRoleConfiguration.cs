using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantIdentityApi.Domain.Entities;

namespace MultiTenantIdentityApi.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for ApplicationRole
/// Configures multi-tenant support, indexes, and constraints
/// </summary>
public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        // Table name
        builder.ToTable("Roles");

        // Configure properties
        builder.Property(r => r.TenantId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Multi-tenant configuration
        // This adds query filters and ensures tenant isolation
        builder.IsMultiTenant();

        // Create composite index for tenant + role name uniqueness
        // This ensures role name uniqueness within a tenant (not globally)
        builder.HasIndex(r => new { r.TenantId, r.NormalizedName })
            .IsUnique()
            .HasDatabaseName("IX_Roles_Tenant_Name");

        // Index on TenantId for query performance
        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_Roles_TenantId");
    }
}
