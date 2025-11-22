using Finbuckle.MultiTenant.EntityFrameworkCore.Stores.EFCoreStore;
using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Models;

namespace MultiTenantIdentityApi.Data;

/// <summary>
/// Database context for storing tenant information (EF Core Store)
/// This is separate from the application DbContext
/// </summary>
public class TenantDbContext : EFCoreStoreDbContext<AppTenantInfo>
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<AppTenantInfo>(entity =>
        {
            entity.ToTable("Tenants");
            
            entity.HasKey(t => t.Id);
            
            entity.Property(t => t.Id)
                .HasMaxLength(64);
            
            entity.Property(t => t.Identifier)
                .HasMaxLength(64)
                .IsRequired();
            
            entity.HasIndex(t => t.Identifier)
                .IsUnique();
            
            entity.Property(t => t.Name)
                .HasMaxLength(256)
                .IsRequired();
            
            entity.Property(t => t.ConnectionString)
                .HasMaxLength(1024);
            
            entity.Property(t => t.Settings)
                .HasColumnType("nvarchar(max)");
            
            entity.Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            // Seed default tenants
            entity.HasData(
                new AppTenantInfo
                {
                    Id = "tenant-1",
                    Identifier = "tenant1",
                    Name = "Tenant One",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new AppTenantInfo
                {
                    Id = "tenant-2",
                    Identifier = "tenant2",
                    Name = "Tenant Two",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        });
    }
}
