namespace MultiTenantIdentityApi.Infrastructure.Configurations;

/// <summary>
/// JWT authentication settings
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// Use RSA certificate for signing (recommended for production)
    /// If true, uses RSA certificate. If false, falls back to symmetric key.
    /// </summary>
    public bool UseRsaCertificate { get; set; } = false;

    /// <summary>
    /// Path to the RSA private key certificate (.pfx file)
    /// Used for signing tokens
    /// </summary>
    public string? RsaPrivateKeyPath { get; set; }

    /// <summary>
    /// Password for the RSA private key certificate
    /// </summary>
    public string? RsaCertificatePassword { get; set; }

    /// <summary>
    /// Path to the RSA public key certificate (.cer or .crt file)
    /// Used for validating tokens (optional - can use private cert for both)
    /// </summary>
    public string? RsaPublicKeyPath { get; set; }

    /// <summary>
    /// Secret key for signing tokens (legacy - symmetric key)
    /// Only used when UseRsaCertificate is false
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration in minutes
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration in days
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
