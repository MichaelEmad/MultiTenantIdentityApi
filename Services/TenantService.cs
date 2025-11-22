using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Data;
using MultiTenantIdentityApi.Models;
using MultiTenantIdentityApi.Models.DTOs;

namespace MultiTenantIdentityApi.Services;

/// <summary>
/// Interface for tenant management operations
/// </summary>
public interface ITenantService
{
    Task<IEnumerable<TenantDto>> GetAllTenantsAsync();
    Task<TenantDto?> GetTenantByIdAsync(string id);
    Task<TenantDto?> GetTenantByIdentifierAsync(string identifier);
    Task<ApiResponse<TenantDto>> CreateTenantAsync(CreateTenantRequest request);
    Task<ApiResponse<TenantDto>> UpdateTenantAsync(string id, UpdateTenantRequest request);
    Task<ApiResponse> DeleteTenantAsync(string id);
    Task<ApiResponse> ActivateTenantAsync(string id);
    Task<ApiResponse> DeactivateTenantAsync(string id);
}

/// <summary>
/// Tenant management service implementation
/// </summary>
public class TenantService : ITenantService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<TenantService> _logger;

    public TenantService(TenantDbContext context, ILogger<TenantService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync()
    {
        return await _context.TenantInfo
            .Select(t => MapToDto(t))
            .ToListAsync();
    }

    public async Task<TenantDto?> GetTenantByIdAsync(string id)
    {
        var tenant = await _context.TenantInfo.FindAsync(id);
        return tenant != null ? MapToDto(tenant) : null;
    }

    public async Task<TenantDto?> GetTenantByIdentifierAsync(string identifier)
    {
        var tenant = await _context.TenantInfo
            .FirstOrDefaultAsync(t => t.Identifier == identifier);
        return tenant != null ? MapToDto(tenant) : null;
    }

    public async Task<ApiResponse<TenantDto>> CreateTenantAsync(CreateTenantRequest request)
    {
        // Check if identifier already exists
        var existingTenant = await _context.TenantInfo
            .AnyAsync(t => t.Identifier == request.Identifier);

        if (existingTenant)
        {
            return ApiResponse<TenantDto>.Failure("A tenant with this identifier already exists.");
        }

        var tenant = new AppTenantInfo
        {
            Id = Guid.NewGuid().ToString(),
            Identifier = request.Identifier,
            Name = request.Name,
            ConnectionString = request.ConnectionString,
            Settings = request.Settings,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.TenantInfo.Add(tenant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantName} ({Identifier}) created successfully", 
            tenant.Name, tenant.Identifier);

        return ApiResponse<TenantDto>.Success(MapToDto(tenant), "Tenant created successfully.");
    }

    public async Task<ApiResponse<TenantDto>> UpdateTenantAsync(string id, UpdateTenantRequest request)
    {
        var tenant = await _context.TenantInfo.FindAsync(id);
        if (tenant == null)
        {
            return ApiResponse<TenantDto>.Failure("Tenant not found.");
        }

        if (request.Name != null)
            tenant.Name = request.Name;

        if (request.ConnectionString != null)
            tenant.ConnectionString = request.ConnectionString;

        if (request.IsActive.HasValue)
            tenant.IsActive = request.IsActive.Value;

        if (request.Settings != null)
            tenant.Settings = request.Settings;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} updated successfully", id);

        return ApiResponse<TenantDto>.Success(MapToDto(tenant), "Tenant updated successfully.");
    }

    public async Task<ApiResponse> DeleteTenantAsync(string id)
    {
        var tenant = await _context.TenantInfo.FindAsync(id);
        if (tenant == null)
        {
            return ApiResponse.Failure("Tenant not found.");
        }

        _context.TenantInfo.Remove(tenant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} deleted successfully", id);

        return ApiResponse.Success("Tenant deleted successfully.");
    }

    public async Task<ApiResponse> ActivateTenantAsync(string id)
    {
        var tenant = await _context.TenantInfo.FindAsync(id);
        if (tenant == null)
        {
            return ApiResponse.Failure("Tenant not found.");
        }

        tenant.IsActive = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} activated", id);

        return ApiResponse.Success("Tenant activated successfully.");
    }

    public async Task<ApiResponse> DeactivateTenantAsync(string id)
    {
        var tenant = await _context.TenantInfo.FindAsync(id);
        if (tenant == null)
        {
            return ApiResponse.Failure("Tenant not found.");
        }

        tenant.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} deactivated", id);

        return ApiResponse.Success("Tenant deactivated successfully.");
    }

    private static TenantDto MapToDto(AppTenantInfo tenant) => new()
    {
        Id = tenant.Id ?? string.Empty,
        Identifier = tenant.Identifier ?? string.Empty,
        Name = tenant.Name ?? string.Empty,
        IsActive = tenant.IsActive,
        CreatedAt = tenant.CreatedAt,
        Settings = tenant.Settings
    };
}
