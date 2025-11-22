using Microsoft.AspNetCore.Identity;

namespace MultiTenantIdentityApi.Domain.Entities;

/// <summary>
/// Custom application user with multi-tenant support
/// Uses Finbuckle.MultiTenant v7 with explicit TenantId property
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// The tenant identifier this user belongs to
    /// Used by Finbuckle multi-tenancy for data isolation
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Date when the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the user was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Indicates if the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
}
