using Microsoft.AspNetCore.Mvc;
using MultiTenantIdentityApi.Application.Common.Models;

namespace MultiTenantIdentityApi.API.Extensions;

/// <summary>
/// Extension methods for Result pattern
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts Result to IActionResult
    /// </summary>
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.Succeeded)
        {
            return new OkObjectResult(new
            {
                succeeded = true,
                message = result.Message
            });
        }

        return new BadRequestObjectResult(new
        {
            succeeded = false,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Converts Result<T> to IActionResult
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.Succeeded)
        {
            return new OkObjectResult(new
            {
                succeeded = true,
                data = result.Data,
                message = result.Message
            });
        }

        return new BadRequestObjectResult(new
        {
            succeeded = false,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Converts Result to IActionResult with custom success status code
    /// </summary>
    public static IActionResult ToActionResult(this Result result, int successStatusCode)
    {
        if (result.Succeeded)
        {
            return new ObjectResult(new
            {
                succeeded = true,
                message = result.Message
            })
            {
                StatusCode = successStatusCode
            };
        }

        return new BadRequestObjectResult(new
        {
            succeeded = false,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Converts Result<T> to IActionResult with custom success status code
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, int successStatusCode)
    {
        if (result.Succeeded)
        {
            return new ObjectResult(new
            {
                succeeded = true,
                data = result.Data,
                message = result.Message
            })
            {
                StatusCode = successStatusCode
            };
        }

        return new BadRequestObjectResult(new
        {
            succeeded = false,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Converts Result<T> to IActionResult with custom mapping for success
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        Func<T, IActionResult> onSuccess)
    {
        if (result.Succeeded && result.Data != null)
        {
            return onSuccess(result.Data);
        }

        return new BadRequestObjectResult(new
        {
            succeeded = false,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Returns NotFound if result failed, otherwise returns Ok with data
    /// </summary>
    public static IActionResult ToActionResultOrNotFound<T>(this Result<T> result)
    {
        if (result.Succeeded && result.Data != null)
        {
            return new OkObjectResult(new
            {
                succeeded = true,
                data = result.Data,
                message = result.Message
            });
        }

        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
        {
            return new NotFoundObjectResult(new
            {
                succeeded = false,
                errors = result.Errors
            });
        }

        return new BadRequestObjectResult(new
        {
            succeeded = false,
            errors = result.Errors
        });
    }
}
