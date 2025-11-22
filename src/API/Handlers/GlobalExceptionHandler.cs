using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MultiTenantIdentityApi.Domain.Exceptions;
using System.Net;

namespace MultiTenantIdentityApi.API.Handlers;

/// <summary>
/// Global exception handler using IExceptionHandler interface (ASP.NET Core 8+)
/// Converts exceptions to ProblemDetails responses (RFC 7807)
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unhandled exception occurred. TraceId: {TraceId}, Path: {Path}",
            httpContext.TraceIdentifier,
            httpContext.Request.Path);

        var problemDetails = CreateProblemDetails(httpContext, exception);

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Exception handled
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            DomainException domainEx => ((int)HttpStatusCode.BadRequest, "Domain Validation Error", domainEx.Message),
            TenantNotFoundException tenantEx => ((int)HttpStatusCode.NotFound, "Tenant Not Found", tenantEx.Message),
            UnauthorizedTenantAccessException authEx => ((int)HttpStatusCode.Forbidden, "Forbidden", authEx.Message),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "Unauthorized", "You are not authorized to access this resource"),
            ArgumentNullException argEx => ((int)HttpStatusCode.BadRequest, "Invalid Request", $"Required parameter is missing: {argEx.ParamName}"),
            ArgumentException argEx => ((int)HttpStatusCode.BadRequest, "Invalid Request", argEx.Message),

            InvalidOperationException invalidEx => ((int)HttpStatusCode.BadRequest, "Invalid Operation", invalidEx.Message),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."
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

        // Add trace ID for debugging
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        // In development, add exception details
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;

            if (exception.StackTrace != null)
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            }

            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    message = exception.InnerException.Message,
                    type = exception.InnerException.GetType().Name
                };
            }
        }

        return problemDetails;
    }

    private static string GetProblemType(int statusCode)
    {
        return statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1", // Bad Request
            401 => "https://tools.ietf.org/html/rfc7235#section-3.1", // Unauthorized
            403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3", // Forbidden
            404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4", // Not Found
            500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1", // Internal Server Error
            _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        };
    }
}
