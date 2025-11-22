namespace MultiTenantIdentityApi.Application.Common.Interfaces;

/// <summary>
/// Service to access current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? TenantId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
