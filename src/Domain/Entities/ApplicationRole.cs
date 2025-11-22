using Microsoft.AspNetCore.Identity;

namespace MultiTenantIdentityApi.Domain.Entities;

/// <summary>
/// Custom application role with multi-tenant support
/// Uses Finbuckle.MultiTenant v7 with explicit TenantId property
/// </summary>
public class ApplicationRole : IdentityRole
{
    /// <summary>
    /// The tenant identifier this role belongs to
    /// Used by Finbuckle multi-tenancy for data isolation
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Description of the role
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Date when the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationRole() : base() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}
