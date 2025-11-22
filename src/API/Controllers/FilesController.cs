using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantIdentityApi.Application.Common.Interfaces;

namespace MultiTenantIdentityApi.API.Controllers;

/// <summary>
/// File management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    // Allowed file extensions
    private readonly string[] _allowedExtensions =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf",
        ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip"
    };

    // Max file size: 10MB
    private const long MaxFileSize = 10 * 1024 * 1024;

    public FilesController(
        IFileStorageService fileStorageService,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(
        IFormFile file,
        [FromQuery] string? folder = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        // Validate file size
        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { error = $"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB" });
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return BadRequest(new { error = $"File type '{extension}' is not allowed" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _fileStorageService.UploadFileWithMetadataAsync(
                stream,
                file.FileName,
                file.ContentType,
                folder);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                filePath = result.FilePath,
                fileUrl = result.FileUrl,
                fileSize = result.FileSize,
                contentType = result.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
            return StatusCode(500, new { error = "An error occurred while uploading the file" });
        }
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    [HttpPost("upload-multiple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMultipleFiles(
        List<IFormFile> files,
        [FromQuery] string? folder = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { error = "No files uploaded" });
        }

        var results = new List<object>();
        var errors = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            // Validate file size
            if (file.Length > MaxFileSize)
            {
                errors.Add($"{file.FileName}: File size exceeds maximum allowed size");
                continue;
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                errors.Add($"{file.FileName}: File type not allowed");
                continue;
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _fileStorageService.UploadFileWithMetadataAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    folder);

                if (result.Success)
                {
                    results.Add(new
                    {
                        fileName = file.FileName,
                        filePath = result.FilePath,
                        fileUrl = result.FileUrl,
                        fileSize = result.FileSize
                    });
                }
                else
                {
                    errors.Add($"{file.FileName}: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                errors.Add($"{file.FileName}: Upload failed");
            }
        }

        return Ok(new
        {
            success = errors.Count == 0,
            uploaded = results,
            errors
        });
    }

    /// <summary>
    /// Download a file
    /// </summary>
    [HttpGet("download/{*filePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(string filePath)
    {
        var result = await _fileStorageService.DownloadFileAsync(filePath);

        if (!result.Success || result.FileStream == null)
        {
            return NotFound(new { error = result.ErrorMessage ?? "File not found" });
        }

        return File(result.FileStream, result.ContentType ?? "application/octet-stream", result.FileName);
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{*filePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(string filePath)
    {
        var deleted = await _fileStorageService.DeleteFileAsync(filePath);

        if (!deleted)
        {
            return NotFound(new { error = "File not found" });
        }

        return Ok(new { success = true, message = "File deleted successfully" });
    }

    /// <summary>
    /// Check if file exists
    /// </summary>
    [HttpHead("{*filePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FileExists(string filePath)
    {
        var exists = await _fileStorageService.FileExistsAsync(filePath);
        return exists ? Ok() : NotFound();
    }

    /// <summary>
    /// Get files in folder
    /// </summary>
    [HttpGet("folder/{*folder}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilesInFolder(string folder)
    {
        var files = await _fileStorageService.GetFilesInFolderAsync(folder);
        return Ok(new { folder, files });
    }
}
