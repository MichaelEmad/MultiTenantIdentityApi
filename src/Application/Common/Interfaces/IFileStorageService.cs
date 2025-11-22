using MultiTenantIdentityApi.Application.Common.Models;
using MultiTenantIdentityApi.Application.DTOs.Files;

namespace MultiTenantIdentityApi.Application.Common.Interfaces;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    Task<Result<FileUploadDto>> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        string? folder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a file from storage
    /// </summary>
    Task<Result<FileDownloadDto>> DownloadFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task<Result> DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<Result<bool>> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file URL (for serving files)
    /// </summary>
    Task<Result<string>> GetFileUrlAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all files in a folder
    /// </summary>
    Task<Result<IEnumerable<FileInfoDto>>> GetFilesInFolderAsync(
        string folder,
        CancellationToken cancellationToken = default);
}
