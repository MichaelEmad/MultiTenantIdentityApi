using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Application.Common.Models;
using MultiTenantIdentityApi.Application.DTOs.Files;

namespace MultiTenantIdentityApi.Infrastructure.Services;

/// <summary>
/// Local file system storage implementation
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _storagePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _storagePath = configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _baseUrl = configuration["FileStorage:BaseUrl"] ?? "/files";

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created storage directory: {Path}", _storagePath);
        }
    }

    public async Task<Result<FileUploadDto>> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileStream == null || !fileStream.CanRead)
            {
                return Result<FileUploadDto>.Failure("Invalid file stream");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Result<FileUploadDto>.Failure("File name is required");
            }

            // Generate unique file name
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            var folderPath = string.IsNullOrEmpty(folder)
                ? _storagePath
                : Path.Combine(_storagePath, folder);

            // Ensure folder exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, uniqueFileName);
            var relativePath = string.IsNullOrEmpty(folder)
                ? uniqueFileName
                : Path.Combine(folder, uniqueFileName);

            // Save file
            long fileSize;
            using (var fileStreamDest = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fileStreamDest, cancellationToken);
                fileSize = fileStreamDest.Length;
            }

            _logger.LogInformation("File uploaded successfully: {FilePath}", relativePath);

            var uploadDto = new FileUploadDto
            {
                FilePath = relativePath,
                FileUrl = $"{_baseUrl}/{relativePath.Replace("\\", "/")}",
                FileSize = fileSize,
                ContentType = contentType,
                UploadedAt = DateTime.UtcNow
            };

            return Result<FileUploadDto>.Success(uploadDto, "File uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            return Result<FileUploadDto>.Failure($"Failed to upload file: {ex.Message}");
        }
    }

    public async Task<Result<FileDownloadDto>> DownloadFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result<FileDownloadDto>.Failure("File path is required");
            }

            var fullPath = Path.Combine(_storagePath, filePath);

            if (!File.Exists(fullPath))
            {
                return Result<FileDownloadDto>.Failure("File not found");
            }

            var fileInfo = new FileInfo(fullPath);
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var downloadDto = new FileDownloadDto
            {
                FileStream = fileStream,
                FileName = Path.GetFileName(filePath),
                ContentType = GetContentType(filePath),
                FileSize = fileInfo.Length
            };

            return Result<FileDownloadDto>.Success(downloadDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
            return Result<FileDownloadDto>.Failure($"Failed to download file: {ex.Message}");
        }
    }

    public Task<Result> DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(Result.Failure("File path is required"));
            }

            var fullPath = Path.Combine(_storagePath, filePath);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult(Result.Failure("File not found"));
            }

            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {FilePath}", filePath);

            return Task.FromResult(Result.Success("File deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return Task.FromResult(Result.Failure($"Failed to delete file: {ex.Message}"));
        }
    }

    public Task<Result<bool>> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(Result<bool>.Failure("File path is required"));
            }

            var fullPath = Path.Combine(_storagePath, filePath);
            var exists = File.Exists(fullPath);

            return Task.FromResult(Result<bool>.Success(exists));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
            return Task.FromResult(Result<bool>.Failure($"Failed to check file existence: {ex.Message}"));
        }
    }

    public Task<Result<string>> GetFileUrlAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(Result<string>.Failure("File path is required"));
            }

            var url = $"{_baseUrl}/{filePath.Replace("\\", "/")}";
            return Task.FromResult(Result<string>.Success(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file URL: {FilePath}", filePath);
            return Task.FromResult(Result<string>.Failure($"Failed to get file URL: {ex.Message}"));
        }
    }

    public Task<Result<IEnumerable<FileInfoDto>>> GetFilesInFolderAsync(
        string folder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var folderPath = string.IsNullOrEmpty(folder)
                ? _storagePath
                : Path.Combine(_storagePath, folder);

            if (!Directory.Exists(folderPath))
            {
                return Task.FromResult(Result<IEnumerable<FileInfoDto>>.Success(
                    Enumerable.Empty<FileInfoDto>(),
                    "Folder not found or empty"));
            }

            var files = Directory.GetFiles(folderPath)
                .Select(f =>
                {
                    var fileInfo = new FileInfo(f);
                    var relativePath = Path.GetRelativePath(_storagePath, f);

                    return new FileInfoDto
                    {
                        FilePath = relativePath,
                        FileName = Path.GetFileName(f),
                        FileUrl = $"{_baseUrl}/{relativePath.Replace("\\", "/")}",
                        FileSize = fileInfo.Length,
                        ContentType = GetContentType(f),
                        CreatedAt = fileInfo.CreationTimeUtc
                    };
                });

            return Task.FromResult(Result<IEnumerable<FileInfoDto>>.Success(files));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files in folder: {Folder}", folder);
            return Task.FromResult(Result<IEnumerable<FileInfoDto>>.Failure($"Failed to get files: {ex.Message}"));
        }
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".zip" => "application/zip",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
