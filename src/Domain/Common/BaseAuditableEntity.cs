namespace MultiTenantIdentityApi.Domain.Common;

/// <summary>
/// Base class for auditable entities
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    /// <summary>
    /// User who created this entity
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// User who last modified this entity
    /// </summary>
    public string? LastModifiedBy { get; set; }
}
