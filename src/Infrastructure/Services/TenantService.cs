using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Application.Common.Models;
using MultiTenantIdentityApi.Application.DTOs.Tenants;
using MultiTenantIdentityApi.Domain.Entities;
using MultiTenantIdentityApi.Infrastructure.Persistence;

namespace MultiTenantIdentityApi.Infrastructure.Services;

/// <summary>
/// Tenant management service implementation
/// </summary>
public class TenantService : ITenantService
{
    private readonly TenantDbContext _context;

    public TenantService(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TenantDto>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        // Check if tenant with same identifier already exists
        var exists = await _context.TenantInfo
            .AnyAsync(t => t.Identifier == request.Identifier, cancellationToken);

        if (exists)
        {
            return Result<TenantDto>.Failure($"Tenant with identifier '{request.Identifier}' already exists.");
        }

        var tenant = new AppTenantInfo
        {
            Id = Guid.NewGuid().ToString(),
            Identifier = request.Identifier,
            Name = request.Name,
            ConnectionString = request.ConnectionString,
            Settings = request.Settings,
            IsActive = true
        };

        _context.TenantInfo.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = new TenantDto
        {
            Id = tenant.Id!,
            Identifier = tenant.Identifier!,
            Name = tenant.Name!,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            Settings = tenant.Settings
        };

        return Result<TenantDto>.Success(dto);
    }

    public async Task<Result<TenantDto>> GetTenantByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.TenantInfo
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
        {
            return Result<TenantDto>.Failure($"Tenant with ID '{id}' not found.");
        }

        var dto = MapToDto(tenant);
        return Result<TenantDto>.Success(dto);
    }

    public async Task<Result<TenantDto>> GetTenantByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.TenantInfo
            .FirstOrDefaultAsync(t => t.Identifier == identifier, cancellationToken);

        if (tenant == null)
        {
            return Result<TenantDto>.Failure($"Tenant with identifier '{identifier}' not found.");
        }

        var dto = MapToDto(tenant);
        return Result<TenantDto>.Success(dto);
    }

    public async Task<Result<IEnumerable<TenantDto>>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _context.TenantInfo.ToListAsync(cancellationToken);
        var dtos = tenants.Select(MapToDto);
        return Result<IEnumerable<TenantDto>>.Success(dtos);
    }

    public async Task<Result<TenantDto>> UpdateTenantAsync(string id, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.TenantInfo
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
        {
            return Result<TenantDto>.Failure($"Tenant with ID '{id}' not found.");
        }

        if (!string.IsNullOrEmpty(request.Name))
            tenant.Name = request.Name;

        if (request.ConnectionString != null)
            tenant.ConnectionString = request.ConnectionString;

        if (request.IsActive.HasValue)
            tenant.IsActive = request.IsActive.Value;

        if (request.Settings != null)
            tenant.Settings = request.Settings;

        await _context.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(tenant);
        return Result<TenantDto>.Success(dto);
    }

    public async Task<Result> DeleteTenantAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.TenantInfo
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
        {
            return Result.Failure($"Tenant with ID '{id}' not found.");
        }

        _context.TenantInfo.Remove(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Tenant deleted successfully.");
    }

    public async Task<Result> ActivateTenantAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.TenantInfo
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
        {
            return Result.Failure($"Tenant with ID '{id}' not found.");
        }

        tenant.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Tenant activated successfully.");
    }

    public async Task<Result> DeactivateTenantAsync(string id, CancellationToken cancellationToken = default)
    {
        var tenant = await _context.TenantInfo
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (tenant == null)
        {
            return Result.Failure($"Tenant with ID '{id}' not found.");
        }

        tenant.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success("Tenant deactivated successfully.");
    }

    private static TenantDto MapToDto(AppTenantInfo tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id!,
            Identifier = tenant.Identifier!,
            Name = tenant.Name!,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            Settings = tenant.Settings
        };
    }
}
