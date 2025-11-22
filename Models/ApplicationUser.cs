using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace MultiTenantIdentityApi.Models;

/// <summary>
/// Custom application user with multi-tenant support
/// </summary>
public class ApplicationUser : IdentityUser, IMultiTenant
{
    /// <summary>
    /// The tenant identifier this user belongs to
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

/// <summary>
/// Custom application role with multi-tenant support
/// </summary>
public class ApplicationRole : IdentityRole, IMultiTenant
{
    /// <summary>
    /// The tenant identifier this role belongs to
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
