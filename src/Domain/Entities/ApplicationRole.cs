using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace MultiTenantIdentityApi.Domain.Entities;

/// <summary>
/// Custom application role with multi-tenant support
/// </summary>
public class ApplicationRole : IdentityRole, IMultiTenant
{
    /// <summary>
    /// The tenant identifier this role belongs to
    /// Required by IMultiTenant interface for Finbuckle multi-tenancy
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
