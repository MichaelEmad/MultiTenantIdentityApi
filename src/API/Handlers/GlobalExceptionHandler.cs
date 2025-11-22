using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MultiTenantIdentityApi.Domain.Entities;
using MultiTenantIdentityApi.Domain.Exceptions;
using System.Diagnostics;
using System.Net;

namespace MultiTenantIdentityApi.API.Handlers;

/// <summary>
/// Global exception handler using IExceptionHandler interface (ASP.NET Core 8+)
/// Provides comprehensive error handling with:
/// - Correlation ID tracking
/// - Structured logging with tenant context
/// - RFC 7807 ProblemDetails responses
/// - Request timing and diagnostics
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    {
        _logger = logger;
        _environment = environment;
        _tenantAccessor = tenantAccessor;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Get correlation ID (from header or generate new)
        var correlationId = GetOrCreateCorrelationId(httpContext);

        // Get tenant information
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        var tenantIdentifier = _tenantAccessor.MultiTenantContext?.TenantInfo?.Identifier;

        // Log structured error with context
        LogException(exception, httpContext, correlationId, tenantId, tenantIdentifier);

        // Create standardized ProblemDetails response
        var problemDetails = CreateProblemDetails(httpContext, exception, correlationId, tenantId);

        // Set response properties
        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        // Write response
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Exception handled
    }

    /// <summary>
    /// Gets correlation ID from request header or generates a new one
    /// </summary>
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check for existing correlation ID in request headers
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        // Check for existing correlation ID in response headers (might be set by middleware)
        if (context.Response.Headers.TryGetValue("X-Correlation-ID", out var responseCorrelationId) &&
            !string.IsNullOrWhiteSpace(responseCorrelationId))
        {
            return responseCorrelationId.ToString();
        }

        // Generate new correlation ID
        var newCorrelationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        context.Response.Headers.Append("X-Correlation-ID", newCorrelationId);
        return newCorrelationId;
    }

    /// <summary>
    /// Logs exception with structured data including tenant context and request information
    /// </summary>
    private void LogException(
        Exception exception,
        HttpContext context,
        string correlationId,
        string? tenantId,
        string? tenantIdentifier)
    {
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var userId = context.User?.Identity?.Name;

        // Structured logging with all relevant context
        _logger.LogError(
            exception,
            "Unhandled exception occurred. " +
            "CorrelationId: {CorrelationId}, " +
            "TenantId: {TenantId}, " +
            "TenantIdentifier: {TenantIdentifier}, " +
            "Path: {Path}, " +
            "Method: {Method}, " +
            "StatusCode: {StatusCode}, " +
            "UserId: {UserId}, " +
            "UserAgent: {UserAgent}, " +
            "RemoteIP: {RemoteIP}, " +
            "ExceptionType: {ExceptionType}",
            correlationId,
            tenantId ?? "N/A",
            tenantIdentifier ?? "N/A",
            requestPath,
            requestMethod,
            GetStatusCodeForException(exception),
            userId ?? "Anonymous",
            userAgent,
            remoteIp,
            exception.GetType().Name);
    }

    /// <summary>
    /// Creates RFC 7807 ProblemDetails response with correlation ID and context
    /// </summary>
    private ProblemDetails CreateProblemDetails(
        HttpContext context,
        Exception exception,
        string correlationId,
        string? tenantId)
    {
        var (statusCode, title, detail) = exception switch
        {
            // Domain-specific exceptions (most specific first)
            TenantNotFoundException tenantEx => (
                (int)HttpStatusCode.NotFound,
                "Tenant Not Found",
                tenantEx.Message
            ),
            UnauthorizedTenantAccessException authEx => (
                (int)HttpStatusCode.Forbidden,
                "Forbidden - Tenant Access Denied",
                authEx.Message
            ),
            DomainException domainEx => (
                (int)HttpStatusCode.BadRequest,
                "Domain Validation Error",
                domainEx.Message
            ),

            // Authorization exceptions
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                "You are not authorized to access this resource"
            ),

            // Argument exceptions (most specific first)
            ArgumentNullException argEx => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Request - Missing Parameter",
                $"Required parameter is missing: {argEx.ParamName}"
            ),
            ArgumentException argEx => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Request - Invalid Argument",
                argEx.Message
            ),

            // Operation exceptions (NotSupportedException before InvalidOperationException)
            NotSupportedException notSupportedEx => (
                (int)HttpStatusCode.BadRequest,
                "Operation Not Supported",
                notSupportedEx.Message
            ),
            InvalidOperationException invalidEx => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Operation",
                invalidEx.Message
            ),

            // Default for unknown exceptions
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please contact support with the correlation ID."
            )
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = GetProblemType(statusCode)
        };

        // Add correlation ID (always)
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow.ToString("o");

        // Add trace ID for ASP.NET Core diagnostics
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        // Add tenant ID if available
        if (!string.IsNullOrEmpty(tenantId))
        {
            problemDetails.Extensions["tenantId"] = tenantId;
        }

        // In development, add detailed exception information
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
            problemDetails.Extensions["exceptionMessage"] = exception.Message;

            if (exception.StackTrace != null)
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace
                    .Split(Environment.NewLine)
                    .Take(10) // Limit stack trace lines
                    .ToArray();
            }

            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    type = exception.InnerException.GetType().FullName,
                    message = exception.InnerException.Message
                };
            }

            // Add request details for debugging
            problemDetails.Extensions["request"] = new
            {
                method = context.Request.Method,
                path = context.Request.Path.ToString(),
                queryString = context.Request.QueryString.ToString(),
                headers = context.Request.Headers
                    .Where(h => !h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(h => h.Key, h => h.Value.ToString())
            };
        }
        else
        {
            // In production, just add a support message
            problemDetails.Extensions["supportMessage"] =
                $"If this error persists, please contact support and provide the correlation ID: {correlationId}";
        }

        return problemDetails;
    }

    /// <summary>
    /// Gets HTTP status code for exception type
    /// </summary>
    private static int GetStatusCodeForException(Exception exception)
    {
        return exception switch
        {
            // More specific exceptions first
            TenantNotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedTenantAccessException => (int)HttpStatusCode.Forbidden,
            DomainException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            NotSupportedException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    /// <summary>
    /// Gets RFC 7807 problem type URI for status code
    /// </summary>
    private static string GetProblemType(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1", // Bad Request
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1", // Unauthorized
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3", // Forbidden
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4", // Not Found
            409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8", // Conflict
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2", // Unprocessable Entity
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1", // Internal Server Error
            503 => "https://tools.ietf.org/html/rfc7231#section-6.6.4", // Service Unavailable
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}
