using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Domain.Entities;
using MultiTenantIdentityApi.Infrastructure.Configurations;

namespace MultiTenantIdentityApi.Infrastructure.Services;

/// <summary>
/// JWT token service implementation
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SymmetricSecurityKey _signingKey;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        UserManager<ApplicationUser> userManager)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    }

    public async Task<string> GenerateAccessTokenAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = await GetClaimsAsync(user, roles);
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        return GenerateAccessToken(claims, expiration);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken, CancellationToken cancellationToken = default)
    {
        // TODO: Implement refresh token validation logic (store in database)
        return Task.FromResult(true);
    }

    public Task StoreRefreshTokenAsync(string userId, string refreshToken, DateTime expiration, CancellationToken cancellationToken = default)
    {
        // TODO: Implement refresh token storage logic (store in database)
        return Task.CompletedTask;
    }

    public Task RevokeRefreshTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement refresh token revocation logic
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<Claim>> GetClaimsAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
        };

        // Add tenant claim - critical for multi-tenant resolution
        if (!string.IsNullOrEmpty(user.TenantId))
        {
            claims.Add(new Claim("tenant_id", user.TenantId));
        }

        // Add custom claims
        if (!string.IsNullOrEmpty(user.FirstName))
        {
            claims.Add(new Claim("first_name", user.FirstName));
        }

        if (!string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim("last_name", user.LastName));
        }

        // Add user claims from Identity
        var userClaims = await _userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return claims;
    }

    private string GenerateAccessToken(IEnumerable<Claim> claims, DateTime expiration)
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
