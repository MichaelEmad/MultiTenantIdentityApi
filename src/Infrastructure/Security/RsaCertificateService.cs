using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;

namespace MultiTenantIdentityApi.Infrastructure.Security;

/// <summary>
/// Helper service for managing RSA certificates for JWT signing
/// </summary>
public interface IRsaCertificateService
{
    /// <summary>
    /// Gets the signing credentials using RSA private key
    /// </summary>
    SigningCredentials GetSigningCredentials();

    /// <summary>
    /// Gets the RSA security key for token validation
    /// </summary>
    RsaSecurityKey GetValidationKey();

    /// <summary>
    /// Gets the certificate thumbprint (for logging/diagnostics)
    /// </summary>
    string GetCertificateThumbprint();
}

/// <summary>
/// Implementation of RSA certificate service
/// </summary>
public class RsaCertificateService : IRsaCertificateService
{
    private readonly X509Certificate2 _certificate;
    private readonly RsaSecurityKey _rsaSecurityKey;
    private readonly SigningCredentials _signingCredentials;

    public RsaCertificateService(string certificatePath, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(certificatePath))
            throw new ArgumentException("Certificate path cannot be null or empty", nameof(certificatePath));

        if (!File.Exists(certificatePath))
            throw new FileNotFoundException($"Certificate file not found at path: {certificatePath}");

        try
        {
            // Load the certificate
            _certificate = string.IsNullOrEmpty(password)
                ? new X509Certificate2(certificatePath)
                : new X509Certificate2(certificatePath, password);

            // Verify the certificate has a private key
            if (!_certificate.HasPrivateKey)
                throw new InvalidOperationException("Certificate does not contain a private key. A private key is required for signing tokens.");

            // Get the RSA key from the certificate
            var rsa = _certificate.GetRSAPrivateKey();
            if (rsa == null)
                throw new InvalidOperationException("Unable to extract RSA private key from certificate.");

            // Create the RSA security key
            _rsaSecurityKey = new RsaSecurityKey(rsa)
            {
                KeyId = _certificate.Thumbprint
            };

            // Create signing credentials with RS256 algorithm
            _signingCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                $"Failed to load certificate from '{certificatePath}'. " +
                "Ensure the certificate is valid and the password is correct.", ex);
        }
    }

    /// <summary>
    /// Alternative constructor for loading from certificate store
    /// </summary>
    public RsaCertificateService(X509Certificate2 certificate)
    {
        _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));

        if (!_certificate.HasPrivateKey)
            throw new InvalidOperationException("Certificate does not contain a private key.");

        var rsa = _certificate.GetRSAPrivateKey();
        if (rsa == null)
            throw new InvalidOperationException("Unable to extract RSA private key from certificate.");

        _rsaSecurityKey = new RsaSecurityKey(rsa)
        {
            KeyId = _certificate.Thumbprint
        };

        _signingCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
    }

    public SigningCredentials GetSigningCredentials() => _signingCredentials;

    public RsaSecurityKey GetValidationKey() => _rsaSecurityKey;

    public string GetCertificateThumbprint() => _certificate.Thumbprint;

    /// <summary>
    /// Factory method to create from certificate store (by thumbprint)
    /// </summary>
    public static RsaCertificateService FromStore(string thumbprint, StoreLocation storeLocation = StoreLocation.CurrentUser)
    {
        using var store = new X509Store(StoreName.My, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
        if (certificates.Count == 0)
            throw new InvalidOperationException($"Certificate with thumbprint '{thumbprint}' not found in certificate store.");

        return new RsaCertificateService(certificates[0]);
    }
}
