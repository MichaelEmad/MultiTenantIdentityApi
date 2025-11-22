namespace MultiTenantIdentityApi.Domain.Exceptions;

/// <summary>
/// Exception thrown when a tenant is not found
/// </summary>
public class TenantNotFoundException : DomainException
{
    public TenantNotFoundException() : base("Tenant not found.") { }

    public TenantNotFoundException(string tenantId)
        : base($"Tenant with ID '{tenantId}' not found.") { }
}
