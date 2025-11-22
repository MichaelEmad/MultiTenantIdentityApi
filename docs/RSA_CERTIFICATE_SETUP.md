# RSA Certificate Setup for JWT Signing

This guide explains how to generate and configure RSA certificates for secure JWT token signing in the Multi-Tenant Identity API.

## Why Use RSA Certificates?

RSA certificates provide several security advantages over symmetric keys:

- **Asymmetric Encryption**: Private key signs tokens, public key verifies them
- **Key Separation**: Signing and validation can use different keys
- **Industry Standard**: Widely accepted for production environments
- **Certificate Management**: Easier to rotate and manage in enterprise environments
- **Distributed Systems**: Public keys can be safely distributed to multiple services

## Generating RSA Certificates

### Option 1: Using OpenSSL (Recommended for Production)

#### 1. Generate a Private Key
```bash
openssl genrsa -out jwt-private.key 2048
```

#### 2. Generate a Certificate Signing Request (CSR)
```bash
openssl req -new -key jwt-private.key -out jwt.csr
```

Fill in the requested information:
- Country Name (2 letter code): US
- State or Province Name: YourState
- Locality Name: YourCity
- Organization Name: YourCompany
- Organizational Unit Name: IT
- Common Name: MultiTenantIdentityApi
- Email Address: admin@yourcompany.com

#### 3. Generate a Self-Signed Certificate (Valid for 365 days)
```bash
openssl x509 -req -days 365 -in jwt.csr -signkey jwt-private.key -out jwt-public.crt
```

#### 4. Create a PFX file (PKCS#12) with Private Key and Certificate
```bash
openssl pkcs12 -export -out jwt-signing.pfx -inkey jwt-private.key -in jwt-public.crt -password pass:YourStrongPassword
```

**Important**: Replace `YourStrongPassword` with a strong password!

### Option 2: Using PowerShell (Windows)

```powershell
# Generate self-signed certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=MultiTenantIdentityApi JWT Signing" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotBefore (Get-Date) `
    -NotAfter (Get-Date).AddYears(2) `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -FriendlyName "JWT Signing Certificate" `
    -HashAlgorithm SHA256 `
    -KeyUsage DigitalSignature, KeyEncipherment, DataEncipherment `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

# Export to PFX file
$password = ConvertTo-SecureString -String "YourStrongPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "jwt-signing.pfx" -Password $password

# Get thumbprint for reference
Write-Host "Certificate Thumbprint: $($cert.Thumbprint)"
```

### Option 3: Using .NET CLI

```bash
dotnet dev-certs https -ep jwt-signing.pfx -p YourStrongPassword --trust
```

**Note**: This generates an HTTPS certificate, but it works for JWT signing as well.

## Configuration

### Development Environment

For development, you can continue using symmetric keys (current setup):

**appsettings.Development.json**:
```json
{
  "JwtSettings": {
    "UseRsaCertificate": false,
    "SecretKey": "YourSecretKeyHereMustBeAtLeast32CharactersLong!!",
    "Issuer": "MultiTenantIdentityApi",
    "Audience": "MultiTenantIdentityApi",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Production Environment

For production, use RSA certificates:

**appsettings.Production.json**:
```json
{
  "JwtSettings": {
    "UseRsaCertificate": true,
    "RsaPrivateKeyPath": "/app/certificates/jwt-signing.pfx",
    "RsaCertificatePassword": "YourStrongPassword",
    "RsaPublicKeyPath": null,
    "Issuer": "MultiTenantIdentityApi",
    "Audience": "MultiTenantIdentityApi",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Important**: Never commit the certificate password to source control! Use environment variables or Azure Key Vault.

### Using Environment Variables

Set environment variables instead of hardcoding the password:

```bash
export JwtSettings__RsaCertificatePassword="YourStrongPassword"
```

Or in Docker:
```yaml
environment:
  - JwtSettings__RsaCertificatePassword=YourStrongPassword
```

Or in `appsettings.Production.json`:
```json
{
  "JwtSettings": {
    "UseRsaCertificate": true,
    "RsaPrivateKeyPath": "/app/certificates/jwt-signing.pfx",
    "RsaCertificatePassword": "${JWT_CERT_PASSWORD}",
    "Issuer": "MultiTenantIdentityApi",
    "Audience": "MultiTenantIdentityApi"
  }
}
```

Then set the environment variable:
```bash
export JWT_CERT_PASSWORD="YourStrongPassword"
```

## Certificate Deployment

### Local Development

1. Generate the certificate (see above)
2. Place `jwt-signing.pfx` in a secure location
3. Update `appsettings.Development.json` with the path
4. Set the password via environment variable or user secrets

### Using .NET User Secrets (Development)

```bash
cd src/API
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:RsaCertificatePassword" "YourStrongPassword"
dotnet user-secrets set "JwtSettings:RsaPrivateKeyPath" "C:/certificates/jwt-signing.pfx"
```

### Docker Container

**Dockerfile**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Create directory for certificates
RUN mkdir -p /app/certificates

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy certificate (use secrets in production!)
COPY certificates/jwt-signing.pfx /app/certificates/

ENTRYPOINT ["dotnet", "MultiTenantIdentityApi.API.dll"]
```

**docker-compose.yml**:
```yaml
version: '3.8'
services:
  api:
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JwtSettings__RsaCertificatePassword=${JWT_CERT_PASSWORD}
    volumes:
      - ./certificates:/app/certificates:ro
    ports:
      - "5000:80"
```

**Important**: Use Docker secrets or Azure Key Vault for production!

### Azure App Service

1. Upload certificate to Azure Key Vault
2. Grant App Service managed identity access to Key Vault
3. Reference certificate from Key Vault in application settings

**appsettings.Production.json**:
```json
{
  "JwtSettings": {
    "UseRsaCertificate": true,
    "RsaPrivateKeyPath": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/jwt-signing-cert/)",
    "RsaCertificatePassword": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/jwt-cert-password/)"
  }
}
```

### Kubernetes

Use Kubernetes secrets:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: jwt-certificate
type: Opaque
data:
  jwt-signing.pfx: <base64-encoded-certificate>
  password: <base64-encoded-password>
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: identity-api
spec:
  template:
    spec:
      containers:
      - name: api
        image: identity-api:latest
        env:
        - name: JwtSettings__RsaCertificatePassword
          valueFrom:
            secretKeyRef:
              name: jwt-certificate
              key: password
        volumeMounts:
        - name: cert-volume
          mountPath: /app/certificates
          readOnly: true
      volumes:
      - name: cert-volume
        secret:
          secretName: jwt-certificate
          items:
          - key: jwt-signing.pfx
            path: jwt-signing.pfx
```

## Certificate Rotation

### Best Practices

1. **Validity Period**: Generate certificates valid for 1-2 years
2. **Rotation Schedule**: Rotate certificates every 6-12 months
3. **Overlap Period**: Deploy new certificate before old one expires
4. **Zero Downtime**: Use multiple valid certificates during rotation

### Rotation Process

1. Generate new certificate
2. Deploy new certificate alongside old one
3. Update configuration to use new certificate
4. Monitor for issues
5. Remove old certificate after grace period

## Troubleshooting

### Common Issues

#### Certificate Not Found
```
FileNotFoundException: Certificate file not found at path: /app/certificates/jwt-signing.pfx
```
**Solution**: Verify the file path and ensure the certificate file exists.

#### Invalid Password
```
CryptographicException: The specified network password is not correct.
```
**Solution**: Verify the certificate password is correct.

#### No Private Key
```
InvalidOperationException: Certificate does not contain a private key.
```
**Solution**: Ensure you're using a PFX file that includes the private key, not just a CRT/CER file.

#### Permission Denied
```
UnauthorizedAccessException: Access to the path is denied.
```
**Solution**: Ensure the application has read permissions for the certificate file.

### Verification

Test that the certificate is loaded correctly:

```bash
# Run the application and check logs
dotnet run --project src/API

# Look for this log message:
# "RSA certificate loaded successfully from: /path/to/jwt-signing.pfx (Thumbprint: XXXXX)"
```

### Testing JWT Tokens

```bash
# Generate a token (after login)
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant1" \
  -d '{"email":"user@example.com","password":"SecureP@ss123"}'

# Verify the token at jwt.io
# The header should show "alg": "RS256" (RSA) instead of "HS256" (HMAC)
```

## Security Best Practices

1. **Never commit certificates to source control**
   - Add `*.pfx`, `*.key`, `*.p12` to `.gitignore`

2. **Use strong passwords**
   - Minimum 16 characters
   - Mix of uppercase, lowercase, numbers, symbols

3. **Store certificates securely**
   - Use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault
   - Encrypt certificate files at rest

4. **Limit access**
   - Only authorized personnel should access certificates
   - Use RBAC to control access

5. **Monitor certificate expiration**
   - Set up alerts 30 days before expiration
   - Automate renewal where possible

6. **Audit certificate usage**
   - Log certificate loading and usage
   - Monitor for unauthorized access attempts

## References

- [RFC 7517 - JSON Web Key (JWK)](https://tools.ietf.org/html/rfc7517)
- [RFC 7518 - JSON Web Algorithms (JWA)](https://tools.ietf.org/html/rfc7518)
- [Microsoft - Certificate Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
- [OpenSSL Documentation](https://www.openssl.org/docs/)
