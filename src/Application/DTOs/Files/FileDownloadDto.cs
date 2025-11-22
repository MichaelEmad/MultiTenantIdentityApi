namespace MultiTenantIdentityApi.Application.DTOs.Files;

/// <summary>
/// DTO for file download result
/// </summary>
public record FileDownloadDto
{
    public Stream FileStream { get; init; } = Stream.Null;
    public string FileName { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public long FileSize { get; init; }
}
