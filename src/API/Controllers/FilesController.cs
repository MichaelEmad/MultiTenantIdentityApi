using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantIdentityApi.API.Extensions;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Application.Common.Models;

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
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadFile(
        IFormFile file,
        [FromQuery] string? folder = null)
    {
        // Validate file
        var validationResult = ValidateFile(file);
        if (!validationResult.Succeeded)
        {
            return validationResult.ToActionResult();
        }

        using var stream = file.OpenReadStream();
        var result = await _fileStorageService.UploadFileAsync(
            stream,
            file.FileName,
            file.ContentType,
            folder);

        return result.ToActionResult();
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    [HttpPost("upload-multiple")]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMultipleFiles(
        List<IFormFile> files,
        [FromQuery] string? folder = null)
    {
        if (files == null || files.Count == 0)
        {
            return Result.Failure("No files provided").ToActionResult();
        }

        var uploadedFiles = new List<object>();
        var errors = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            // Validate file
            var validationResult = ValidateFile(file);
            if (!validationResult.Succeeded)
            {
                errors.Add($"{file.FileName}: {string.Join(", ", validationResult.Errors)}");
                continue;
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _fileStorageService.UploadFileAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    folder);

                if (result.Succeeded && result.Data != null)
                {
                    uploadedFiles.Add(new
                    {
                        fileName = file.FileName,
                        result.Data.FilePath,
                        result.Data.FileUrl,
                        result.Data.FileSize
                    });
                }
                else
                {
                    errors.Add($"{file.FileName}: {string.Join(", ", result.Errors)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file.FileName);
                errors.Add($"{file.FileName}: Upload failed");
            }
        }

        var multiUploadResult = new
        {
            succeeded = errors.Count == 0,
            uploadedFiles,
            errors
        };

        return Ok(multiUploadResult);
    }

    /// <summary>
    /// Download a file
    /// </summary>
    [HttpGet("download/{*filePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(string filePath)
    {
        var result = await _fileStorageService.DownloadFileAsync(filePath);

        if (!result.Succeeded || result.Data == null)
        {
            return result.ToActionResultOrNotFound();
        }

        return File(
            result.Data.FileStream,
            result.Data.ContentType ?? "application/octet-stream",
            result.Data.FileName);
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{*filePath}")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(string filePath)
    {
        var result = await _fileStorageService.DeleteFileAsync(filePath);
        return result.ToActionResult();
    }

    /// <summary>
    /// Check if file exists
    /// </summary>
    [HttpHead("{*filePath}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FileExists(string filePath)
    {
        var result = await _fileStorageService.FileExistsAsync(filePath);

        if (!result.Succeeded)
        {
            return BadRequest();
        }

        return result.Data == true ? Ok() : NotFound();
    }

    /// <summary>
    /// Get files in folder
    /// </summary>
    [HttpGet("folder/{*folder}")]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilesInFolder(string folder)
    {
        var result = await _fileStorageService.GetFilesInFolderAsync(folder);
        return result.ToActionResult();
    }

    private Result ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Result.Failure("No file uploaded or file is empty");
        }

        if (file.Length > MaxFileSize)
        {
            return Result.Failure($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)}MB");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return Result.Failure($"File type '{extension}' is not allowed");
        }

        return Result.Success();
    }
}
