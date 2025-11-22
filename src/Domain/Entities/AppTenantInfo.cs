using Finbuckle.MultiTenant.Abstractions;

namespace MultiTenantIdentityApi.Domain.Entities;

/// <summary>
/// Custom tenant information stored in the database
/// </summary>
public class AppTenantInfo : ITenantInfo
{
    /// <summary>
    /// Unique identifier for the tenant
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Unique identifier used in claims and routing
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Display name of the tenant
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Tenant-specific connection string (optional - for separate databases per tenant)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Indicates if the tenant is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date when the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional tenant-specific settings (JSON)
    /// </summary>
    public string? Settings { get; set; }
}
