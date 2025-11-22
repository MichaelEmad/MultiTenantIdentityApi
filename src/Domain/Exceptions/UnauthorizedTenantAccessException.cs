namespace MultiTenantIdentityApi.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to access resources from another tenant
/// </summary>
public class UnauthorizedTenantAccessException : DomainException
{
    public UnauthorizedTenantAccessException()
        : base("Unauthorized access to tenant resources.") { }

    public UnauthorizedTenantAccessException(string message) : base(message) { }
}
