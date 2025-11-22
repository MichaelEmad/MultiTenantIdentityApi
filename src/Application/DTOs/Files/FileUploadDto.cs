namespace MultiTenantIdentityApi.Application.DTOs.Files;

/// <summary>
/// DTO for file upload result
/// </summary>
public record FileUploadDto
{
    public string FilePath { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string? ContentType { get; init; }
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}
