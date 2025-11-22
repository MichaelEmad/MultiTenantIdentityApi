using MultiTenantIdentityApi.Application.Common.Models;
using MultiTenantIdentityApi.Application.DTOs.Tenants;

namespace MultiTenantIdentityApi.Application.Common.Interfaces;

/// <summary>
/// Tenant management service interface
/// </summary>
public interface ITenantService
{
    Task<Result<TenantDto>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> GetTenantByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> GetTenantByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<TenantDto>>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> UpdateTenantAsync(string id, UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteTenantAsync(string id, CancellationToken cancellationToken = default);
    Task<Result> ActivateTenantAsync(string id, CancellationToken cancellationToken = default);
    Task<Result> DeactivateTenantAsync(string id, CancellationToken cancellationToken = default);
}
