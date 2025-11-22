namespace MultiTenantIdentityApi.Domain.Common;

/// <summary>
/// Base class for all domain entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Date when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date when the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
