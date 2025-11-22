using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiTenantIdentityApi.Application.Common.Interfaces;

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

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await UploadFileWithMetadataAsync(
                fileStream, fileName, contentType, folder, null, cancellationToken);

            return result.Success ? result.FilePath! : throw new InvalidOperationException(result.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<FileUploadResult> UploadFileWithMetadataAsync(
        Stream fileStream,
        string fileName,
        string? contentType = null,
        string? folder = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
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
            using (var fileStreamDest = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fileStreamDest, cancellationToken);
            }

            var fileInfo = new FileInfo(filePath);

            _logger.LogInformation("File uploaded successfully: {FilePath}", relativePath);

            return new FileUploadResult
            {
                Success = true,
                FilePath = relativePath,
                FileUrl = $"{_baseUrl}/{relativePath.Replace("\\", "/")}",
                FileSize = fileInfo.Length,
                ContentType = contentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", fileName);
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<FileDownloadResult> DownloadFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_storagePath, filePath);

            if (!File.Exists(fullPath))
            {
                return new FileDownloadResult
                {
                    Success = false,
                    ErrorMessage = "File not found"
                };
            }

            var fileInfo = new FileInfo(fullPath);
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return new FileDownloadResult
            {
                Success = true,
                FileStream = fileStream,
                FileName = Path.GetFileName(filePath),
                ContentType = GetContentType(filePath),
                FileSize = fileInfo.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
            return new FileDownloadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<bool> DeleteFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_storagePath, filePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {FilePath}", filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, filePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<string> GetFileUrlAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/{filePath.Replace("\\", "/")}";
        return Task.FromResult(url);
    }

    public Task<IEnumerable<string>> GetFilesInFolderAsync(
        string folder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var folderPath = Path.Combine(_storagePath, folder);

            if (!Directory.Exists(folderPath))
            {
                return Task.FromResult(Enumerable.Empty<string>());
            }

            var files = Directory.GetFiles(folderPath)
                .Select(f => Path.GetRelativePath(_storagePath, f));

            return Task.FromResult(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files in folder: {Folder}", folder);
            return Task.FromResult(Enumerable.Empty<string>());
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
