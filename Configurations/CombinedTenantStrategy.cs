using Finbuckle.MultiTenant.Abstractions;

namespace MultiTenantIdentityApi.Configurations;

/// <summary>
/// Combined tenant resolution strategy that checks multiple sources
/// Priority: Claim -> Header -> Route -> Query
/// </summary>
public class CombinedTenantStrategy : IMultiTenantStrategy
{
    private readonly string _claimType;
    private readonly string _headerName;
    private readonly string _routeParam;
    private readonly string _queryParam;

    public CombinedTenantStrategy(
        string claimType = "tenant_id",
        string headerName = "X-Tenant-Id",
        string routeParam = "tenant",
        string queryParam = "tenant")
    {
        _claimType = claimType;
        _headerName = headerName;
        _routeParam = routeParam;
        _queryParam = queryParam;
    }

    public async Task<string?> GetIdentifierAsync(object context)
    {
        if (context is not HttpContext httpContext)
        {
            return null;
        }

        // 1. First priority: Check authenticated user's claims
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var claimValue = httpContext.User.FindFirst(_claimType)?.Value;
            if (!string.IsNullOrEmpty(claimValue))
            {
                return claimValue;
            }
        }

        // 2. Second priority: Check header (useful for login/register)
        if (httpContext.Request.Headers.TryGetValue(_headerName, out var headerValue))
        {
            var header = headerValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(header))
            {
                return header;
            }
        }

        // 3. Third priority: Check route parameter
        var routeValue = httpContext.Request.RouteValues[_routeParam]?.ToString();
        if (!string.IsNullOrEmpty(routeValue))
        {
            return routeValue;
        }

        // 4. Fourth priority: Check query string
        if (httpContext.Request.Query.TryGetValue(_queryParam, out var queryValue))
        {
            var query = queryValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(query))
            {
                return query;
            }
        }

        return await Task.FromResult<string?>(null);
    }
}

/// <summary>
/// Extension methods for configuring combined tenant strategy
/// </summary>
public static class CombinedTenantStrategyExtensions
{
    /// <summary>
    /// Adds combined tenant resolution strategy
    /// </summary>
    public static MultiTenantBuilder<T> WithCombinedStrategy<T>(
        this MultiTenantBuilder<T> builder,
        string claimType = "tenant_id",
        string headerName = "X-Tenant-Id",
        string routeParam = "tenant",
        string queryParam = "tenant")
        where T : class, ITenantInfo, new()
    {
        return builder.WithStrategy<CombinedTenantStrategy>(
            ServiceLifetime.Scoped,
            claimType,
            headerName,
            routeParam,
            queryParam);
    }
}
