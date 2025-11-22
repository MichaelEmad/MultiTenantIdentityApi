namespace MultiTenantIdentityApi.Application.DTOs.Files;

/// <summary>
/// DTO for file information
/// </summary>
public record FileInfoDto
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string? ContentType { get; init; }
    public DateTime CreatedAt { get; init; }
}
