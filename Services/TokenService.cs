using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MultiTenantIdentityApi.Configurations;
using MultiTenantIdentityApi.Models;

namespace MultiTenantIdentityApi.Services;

/// <summary>
/// Interface for JWT token operations
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates access and refresh tokens for a user
    /// </summary>
    Task<(string AccessToken, string RefreshToken, DateTime AccessExpiration, DateTime RefreshExpiration)> 
        GenerateTokensAsync(ApplicationUser user);
    
    /// <summary>
    /// Validates a JWT token and returns the claims principal
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true);
    
    /// <summary>
    /// Generates a new refresh token
    /// </summary>
    string GenerateRefreshToken();
}

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

    public async Task<(string AccessToken, string RefreshToken, DateTime AccessExpiration, DateTime RefreshExpiration)> 
        GenerateTokensAsync(ApplicationUser user)
    {
        var accessExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var refreshExpiration = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var claims = await GetClaimsAsync(user);
        var accessToken = GenerateAccessToken(claims, accessExpiration);
        var refreshToken = GenerateRefreshToken();

        return (accessToken, refreshToken, accessExpiration, refreshExpiration);
    }

    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = validateLifetime,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task<IEnumerable<Claim>> GetClaimsAsync(ApplicationUser user)
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
        var roles = await _userManager.GetRolesAsync(user);
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
