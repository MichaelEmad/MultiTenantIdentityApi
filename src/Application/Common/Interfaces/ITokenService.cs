using MultiTenantIdentityApi.Domain.Entities;

namespace MultiTenantIdentityApi.Application.Common.Interfaces;

/// <summary>
/// JWT token service interface
/// </summary>
public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(ApplicationUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken, CancellationToken cancellationToken = default);
    Task StoreRefreshTokenAsync(string userId, string refreshToken, DateTime expiration, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string userId, CancellationToken cancellationToken = default);
}
