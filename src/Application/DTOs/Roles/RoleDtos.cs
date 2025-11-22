using System.ComponentModel.DataAnnotations;

namespace MultiTenantIdentityApi.Application.DTOs.Roles;

/// <summary>
/// Create role request
/// </summary>
public record CreateRoleRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }
}

/// <summary>
/// Assign role to user request
/// </summary>
public record AssignRoleRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public string RoleName { get; init; } = string.Empty;
}

/// <summary>
/// Role DTO
/// </summary>
public record RoleDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? TenantId { get; init; }
}
