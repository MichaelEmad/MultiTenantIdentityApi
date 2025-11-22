namespace MultiTenantIdentityApi.Application.Common.Interfaces;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        string? folder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a file with metadata
    /// </summary>
    Task<FileUploadResult> UploadFileWithMetadataAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        string? folder = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file from storage
    /// </summary>
    Task<FileDownloadResult> DownloadFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task<bool> DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file URL (for serving files)
    /// </summary>
    Task<string> GetFileUrlAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all files in a folder
    /// </summary>
    Task<IEnumerable<string>> GetFilesInFolderAsync(
        string folder,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of file upload operation
/// </summary>
public record FileUploadResult
{
    public bool Success { get; init; }
    public string? FilePath { get; init; }
    public string? FileUrl { get; init; }
    public long FileSize { get; init; }
    public string? ContentType { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of file download operation
/// </summary>
public record FileDownloadResult
{
    public bool Success { get; init; }
    public Stream? FileStream { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public long FileSize { get; init; }
    public string? ErrorMessage { get; init; }
}
