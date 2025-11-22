using System.Net;
using System.Text.Json;
using Finbuckle.MultiTenant.Abstractions;
using MultiTenantIdentityApi.Models;
using MultiTenantIdentityApi.Models.DTOs;

namespace MultiTenantIdentityApi.Middleware;

/// <summary>
/// Middleware to validate tenant and enforce tenant-based access
/// </summary>
public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantValidationMiddleware> _logger;
    
    // Paths that don't require tenant validation
    private static readonly string[] ExcludedPaths = 
    [
        "/api/tenants",
        "/swagger",
        "/health",
        "/.well-known"
    ];

    public TenantValidationMiddleware(RequestDelegate next, ILogger<TenantValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, 
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip validation for excluded paths
        if (ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var tenantContext = tenantAccessor.MultiTenantContext;
        var tenant = tenantContext?.TenantInfo;

        // For authentication endpoints, tenant header is required but not validation
        if (path.StartsWith("/api/auth/login") || path.StartsWith("/api/auth/register"))
        {
            if (tenant == null)
            {
                _logger.LogWarning("Tenant not identified for authentication request to {Path}", path);
                await WriteErrorResponse(context, HttpStatusCode.BadRequest, 
                    "Tenant identification required. Please provide X-Tenant-Id header.");
                return;
            }

            if (!tenant.IsActive)
            {
                _logger.LogWarning("Inactive tenant {TenantId} attempted authentication", tenant.Id);
                await WriteErrorResponse(context, HttpStatusCode.Forbidden, 
                    "Tenant is inactive. Please contact support.");
                return;
            }
        }

        // For other protected endpoints
        if (context.User.Identity?.IsAuthenticated == true && tenant != null)
        {
            // Verify user's tenant claim matches resolved tenant
            var userTenantClaim = context.User.FindFirst("tenant_id")?.Value;
            
            if (!string.IsNullOrEmpty(userTenantClaim) && userTenantClaim != tenant.Id)
            {
                _logger.LogWarning("Tenant mismatch: User {UserId} has tenant {UserTenant} but tried to access {RequestTenant}",
                    context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    userTenantClaim,
                    tenant.Id);
                
                await WriteErrorResponse(context, HttpStatusCode.Forbidden, 
                    "Access denied. Invalid tenant context.");
                return;
            }
        }

        await _next(context);
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiResponse
        {
            Succeeded = false,
            Errors = [message]
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

/// <summary>
/// Extension method for adding tenant validation middleware
/// </summary>
public static class TenantValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantValidationMiddleware>();
    }
}
