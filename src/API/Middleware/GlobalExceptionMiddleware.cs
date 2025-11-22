using MultiTenantIdentityApi.Application.Common.Models;
using MultiTenantIdentityApi.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace MultiTenantIdentityApi.API.Middleware;

/// <summary>
/// Global exception handling middleware that converts exceptions to Result pattern responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, result) = exception switch
        {
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                Result.Failure(domainEx.Message)
            ),
            TenantNotFoundException tenantEx => (
                HttpStatusCode.NotFound,
                Result.Failure(tenantEx.Message)
            ),
            UnauthorizedTenantAccessException authEx => (
                HttpStatusCode.Forbidden,
                Result.Failure(authEx.Message)
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                Result.Failure("Unauthorized access")
            ),
            ArgumentNullException argEx => (
                HttpStatusCode.BadRequest,
                Result.Failure($"Required parameter is missing: {argEx.ParamName}")
            ),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                Result.Failure(argEx.Message)
            ),
            InvalidOperationException invalidEx => (
                HttpStatusCode.BadRequest,
                Result.Failure(invalidEx.Message)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                _environment.IsDevelopment()
                    ? Result.Failure(new[] {
                        "An internal server error occurred",
                        exception.Message,
                        exception.StackTrace ?? string.Empty
                    })
                    : Result.Failure("An internal server error occurred. Please try again later.")
            )
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var response = new
        {
            result.Succeeded,
            result.Message,
            result.Errors,
            StatusCode = (int)statusCode,
            TraceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
/// Extension method to register the global exception middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
