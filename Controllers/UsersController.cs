using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Models;
using MultiTenantIdentityApi.Models.DTOs;

namespace MultiTenantIdentityApi.Controllers;

/// <summary>
/// User management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    private string? CurrentTenantId => _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

    /// <summary>
    /// Get all users in the current tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = _userManager.Users
            .Where(u => u.TenantId == CurrentTenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => 
                (u.Email != null && u.Email.Contains(search)) ||
                (u.FirstName != null && u.FirstName.Contains(search)) ||
                (u.LastName != null && u.LastName.Contains(search)) ||
                (u.UserName != null && u.UserName.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                TenantId = u.TenantId
            })
            .ToListAsync();

        return Ok(new
        {
            Data = users,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null || user.TenantId != CurrentTenantId)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TenantId = user.TenantId,
            Roles = roles
        });
    }

    /// <summary>
    /// Activate a user account
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null || user.TenantId != CurrentTenantId)
        {
            return NotFound(ApiResponse.Failure("User not found."));
        }

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse.Failure(result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("User {UserId} activated", id);

        return Ok(ApiResponse.Success("User activated successfully."));
    }

    /// <summary>
    /// Deactivate a user account
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null || user.TenantId != CurrentTenantId)
        {
            return NotFound(ApiResponse.Failure("User not found."));
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse.Failure(result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("User {UserId} deactivated", id);

        return Ok(ApiResponse.Success("User deactivated successfully."));
    }

    /// <summary>
    /// Lock out a user account
    /// </summary>
    [HttpPost("{id}/lockout")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lockout(string id, [FromQuery] int? durationMinutes = null)
    {
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null || user.TenantId != CurrentTenantId)
        {
            return NotFound(ApiResponse.Failure("User not found."));
        }

        var lockoutEnd = durationMinutes.HasValue
            ? DateTimeOffset.UtcNow.AddMinutes(durationMinutes.Value)
            : DateTimeOffset.MaxValue;

        var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse.Failure(result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("User {UserId} locked out until {LockoutEnd}", id, lockoutEnd);

        return Ok(ApiResponse.Success($"User locked out until {lockoutEnd:yyyy-MM-dd HH:mm:ss} UTC."));
    }

    /// <summary>
    /// Unlock a user account
    /// </summary>
    [HttpPost("{id}/unlock")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null || user.TenantId != CurrentTenantId)
        {
            return NotFound(ApiResponse.Failure("User not found."));
        }

        var result = await _userManager.SetLockoutEndDateAsync(user, null);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse.Failure(result.Errors.Select(e => e.Description)));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("User {UserId} unlocked", id);

        return Ok(ApiResponse.Success("User unlocked successfully."));
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null || user.TenantId != CurrentTenantId)
        {
            return NotFound(ApiResponse.Failure("User not found."));
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(ApiResponse.Failure(result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("User {UserId} deleted", id);

        return Ok(ApiResponse.Success("User deleted successfully."));
    }
}
