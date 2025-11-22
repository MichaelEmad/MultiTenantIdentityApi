using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MultiTenantIdentityApi.Infrastructure.MultiTenancy;

/// <summary>
/// Custom claim-based tenant resolution strategy
/// Resolves the tenant from JWT claims or authenticated user claims
/// </summary>
public class ClaimStrategy : IMultiTenantStrategy
{
    private readonly string _claimType;

    /// <summary>
    /// Creates a new claim strategy with the specified claim type
    /// </summary>
    /// <param name="claimType">The claim type to use for tenant resolution</param>
    public ClaimStrategy(string claimType)
    {
        _claimType = claimType ?? throw new ArgumentNullException(nameof(claimType));
    }

    /// <summary>
    /// Default claim type used for tenant identification
    /// </summary>
    public const string DefaultClaimType = "tenant_id";

    public async Task<string?> GetIdentifierAsync(object context)
    {
        if (context is not HttpContext httpContext)
        {
            return null;
        }

        // Check if user is authenticated
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return null;
        }

        // Get tenant identifier from claims
        var tenantClaim = httpContext.User.FindFirst(_claimType);

        return await Task.FromResult(tenantClaim?.Value);
    }
}

/// <summary>
/// Extension methods for configuring claim-based tenant strategy
/// </summary>
public static class ClaimStrategyExtensions
{
    /// <summary>
    /// Adds claim-based tenant resolution strategy
    /// </summary>
    /// <param name="builder">The multi-tenant builder</param>
    /// <param name="claimType">The claim type to use (defaults to "tenant_id")</param>
    /// <returns>The builder for chaining</returns>
    public static MultiTenantBuilder<T> WithClaimStrategy<T>(
        this MultiTenantBuilder<T> builder,
        string claimType = ClaimStrategy.DefaultClaimType)
        where T : class, ITenantInfo, new()
    {
        return builder.WithStrategy<ClaimStrategy>(ServiceLifetime.Scoped, claimType);
    }
}
