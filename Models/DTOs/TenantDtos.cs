using System.ComponentModel.DataAnnotations;

namespace MultiTenantIdentityApi.Models.DTOs;

/// <summary>
/// Create tenant request
/// </summary>
public record CreateTenantRequest
{
    [Required]
    [MaxLength(64)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Identifier can only contain lowercase letters, numbers, and hyphens")]
    public string Identifier { get; init; } = string.Empty;
    
    [Required]
    [MaxLength(256)]
    public string Name { get; init; } = string.Empty;
    
    [MaxLength(1024)]
    public string? ConnectionString { get; init; }
    
    public string? Settings { get; init; }
}

/// <summary>
/// Update tenant request
/// </summary>
public record UpdateTenantRequest
{
    [MaxLength(256)]
    public string? Name { get; init; }
    
    [MaxLength(1024)]
    public string? ConnectionString { get; init; }
    
    public bool? IsActive { get; init; }
    
    public string? Settings { get; init; }
}

/// <summary>
/// Tenant data transfer object
/// </summary>
public record TenantDto
{
    public string Id { get; init; } = string.Empty;
    public string Identifier { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Settings { get; init; }
}
