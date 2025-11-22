using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Domain.Entities;
using MultiTenantIdentityApi.Infrastructure.Configurations;
using MultiTenantIdentityApi.Infrastructure.Security;

namespace MultiTenantIdentityApi.Infrastructure.Services;

/// <summary>
/// JWT token service implementation with support for both symmetric and RSA signing
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TokenService> _logger;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        UserManager<ApplicationUser> userManager,
        ILogger<TokenService> logger,
        IRsaCertificateService? rsaCertificateService = null)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _logger = logger;

        // Determine which signing method to use
        if (_jwtSettings.UseRsaCertificate)
        {
            if (rsaCertificateService == null)
            {
                throw new InvalidOperationException(
                    "RSA certificate service is not configured. " +
                    "Either configure RSA certificates or set UseRsaCertificate to false.");
            }

            _signingCredentials = rsaCertificateService.GetSigningCredentials();
            _logger.LogInformation(
                "TokenService initialized with RSA certificate signing (Thumbprint: {Thumbprint})",
                rsaCertificateService.GetCertificateThumbprint());
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_jwtSettings.SecretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured.");
            }

            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            _signingCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);
            _logger.LogWarning(
                "TokenService initialized with symmetric key signing. " +
                "Consider using RSA certificates for production environments.");
        }
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
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: _signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
