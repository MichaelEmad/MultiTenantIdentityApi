using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Domain.Entities;
using MultiTenantIdentityApi.Infrastructure.Configurations;
using MultiTenantIdentityApi.Infrastructure.MultiTenancy;
using MultiTenantIdentityApi.Infrastructure.Persistence;
using MultiTenantIdentityApi.Infrastructure.Security;
using MultiTenantIdentityApi.Infrastructure.Services;

namespace MultiTenantIdentityApi.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure (
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Database contexts
        services.AddDatabaseContexts(configuration);

        // Add Multi-tenancy
        services.AddMultiTenancy(configuration);

        // Add Identity
        services.AddIdentityServices();

        // Add JWT Authentication
        services.AddJwtAuthentication(configuration);

        // Add Application Services
        services.AddApplicationServices();

        return services;
    }

    private static IServiceCollection AddDatabaseContexts (
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Tenant Store DbContext (for storing tenant information)
        services.AddDbContext<TenantDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Main Application DbContext with multi-tenant support
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    private static IServiceCollection AddMultiTenancy (
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMultiTenant<AppTenantInfo>()
            .WithEFCoreStore<TenantDbContext, AppTenantInfo>()
            .WithClaimStrategy("tenant_id")
            .WithHeaderStrategy("X-Tenant-Id")
            .WithRouteStrategy("tenant")
            .WithQueryStringStrategy("tenant");

        return services;
    }

    private static IServiceCollection AddIdentityServices (this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 6;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = false; // Email uniqueness is per-tenant

            // Sign-in settings
            options.SignIn.RequireConfirmedEmail = false; // Set to true in production
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication (
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured");

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Configure RSA certificate service if enabled
        SecurityKey validationKey;

        if (jwtSettings.UseRsaCertificate)
        {
            if (string.IsNullOrWhiteSpace(jwtSettings.RsaPrivateKeyPath))
            {
                throw new InvalidOperationException(
                    "UseRsaCertificate is enabled but RsaPrivateKeyPath is not configured.");
            }

            // Register RSA certificate service as singleton (certificate is reused)
            services.AddSingleton<IRsaCertificateService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RsaCertificateService>>();

                try
                {
                    var certificateService = new RsaCertificateService(
                        jwtSettings.RsaPrivateKeyPath,
                        jwtSettings.RsaCertificatePassword);

                    logger.LogInformation(
                        "RSA certificate loaded successfully from: {Path} (Thumbprint: {Thumbprint})",
                        jwtSettings.RsaPrivateKeyPath,
                        certificateService.GetCertificateThumbprint());

                    return certificateService;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to load RSA certificate from: {Path}",
                        jwtSettings.RsaPrivateKeyPath);
                    throw;
                }
            });

            // Use RSA public key for token validation
            var rsaService = services.BuildServiceProvider().GetRequiredService<IRsaCertificateService>();
            validationKey = rsaService.GetValidationKey();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
            {
                throw new InvalidOperationException(
                    "JWT SecretKey is required when RSA certificates are not used.");
            }

            // Use symmetric key
            validationKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false; // Set to true in production
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = validationKey,
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }

    private static IServiceCollection AddApplicationServices (this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();

        // Register file storage service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Register Excel export service
        services.AddScoped<IExcelExportService, ExcelExportService>();

        return services;
    }
}
