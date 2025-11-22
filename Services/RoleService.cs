using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Models;
using MultiTenantIdentityApi.Models.DTOs;

namespace MultiTenantIdentityApi.Services;

/// <summary>
/// Interface for role management operations
/// </summary>
public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(string id);
    Task<ApiResponse<RoleDto>> CreateRoleAsync(CreateRoleRequest request);
    Task<ApiResponse> DeleteRoleAsync(string id);
    Task<ApiResponse> AssignRoleToUserAsync(AssignRoleRequest request);
    Task<ApiResponse> RemoveRoleFromUserAsync(AssignRoleRequest request);
    Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName);
}

/// <summary>
/// Role management service implementation
/// </summary>
public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    private string? CurrentTenantId => _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        return await _roleManager.Roles
            .Where(r => r.TenantId == CurrentTenantId)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Description = r.Description,
                TenantId = r.TenantId
            })
            .ToListAsync();
    }

    public async Task<RoleDto?> GetRoleByIdAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null || role.TenantId != CurrentTenantId)
        {
            return null;
        }

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            TenantId = role.TenantId
        };
    }

    public async Task<ApiResponse<RoleDto>> CreateRoleAsync(CreateRoleRequest request)
    {
        if (string.IsNullOrEmpty(CurrentTenantId))
        {
            return ApiResponse<RoleDto>.Failure("Tenant not identified.");
        }

        // Check if role already exists in tenant
        var existingRole = await _roleManager.Roles
            .AnyAsync(r => r.NormalizedName == request.Name.ToUpperInvariant() 
                           && r.TenantId == CurrentTenantId);

        if (existingRole)
        {
            return ApiResponse<RoleDto>.Failure("A role with this name already exists.");
        }

        var role = new ApplicationRole(request.Name)
        {
            Description = request.Description,
            TenantId = CurrentTenantId
        };

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return ApiResponse<RoleDto>.Failure(result.Errors.Select(e => e.Description));
        }

        _logger.LogInformation("Role {RoleName} created for tenant {TenantId}", 
            role.Name, CurrentTenantId);

        return ApiResponse<RoleDto>.Success(new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            TenantId = role.TenantId
        }, "Role created successfully.");
    }

    public async Task<ApiResponse> DeleteRoleAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null || role.TenantId != CurrentTenantId)
        {
            return ApiResponse.Failure("Role not found.");
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            return ApiResponse.Failure(result.Errors.Select(e => e.Description));
        }

        _logger.LogInformation("Role {RoleName} deleted from tenant {TenantId}", 
            role.Name, CurrentTenantId);

        return ApiResponse.Success("Role deleted successfully.");
    }

    public async Task<ApiResponse> AssignRoleToUserAsync(AssignRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null || user.TenantId != CurrentTenantId)
        {
            return ApiResponse.Failure("User not found.");
        }

        var role = await _roleManager.Roles
            .FirstOrDefaultAsync(r => r.Name == request.RoleName && r.TenantId == CurrentTenantId);

        if (role == null)
        {
            return ApiResponse.Failure("Role not found.");
        }

        if (await _userManager.IsInRoleAsync(user, request.RoleName))
        {
            return ApiResponse.Failure("User is already in this role.");
        }

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);

        if (!result.Succeeded)
        {
            return ApiResponse.Failure(result.Errors.Select(e => e.Description));
        }

        _logger.LogInformation("User {UserId} assigned to role {RoleName}", 
            request.UserId, request.RoleName);

        return ApiResponse.Success("User assigned to role successfully.");
    }

    public async Task<ApiResponse> RemoveRoleFromUserAsync(AssignRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null || user.TenantId != CurrentTenantId)
        {
            return ApiResponse.Failure("User not found.");
        }

        if (!await _userManager.IsInRoleAsync(user, request.RoleName))
        {
            return ApiResponse.Failure("User is not in this role.");
        }

        var result = await _userManager.RemoveFromRoleAsync(user, request.RoleName);

        if (!result.Succeeded)
        {
            return ApiResponse.Failure(result.Errors.Select(e => e.Description));
        }

        _logger.LogInformation("User {UserId} removed from role {RoleName}", 
            request.UserId, request.RoleName);

        return ApiResponse.Success("User removed from role successfully.");
    }

    public async Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        
        return users
            .Where(u => u.TenantId == CurrentTenantId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                TenantId = u.TenantId
            });
    }
}
