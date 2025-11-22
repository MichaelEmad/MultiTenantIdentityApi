using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantIdentityApi.API.Extensions;
using MultiTenantIdentityApi.Application.Common.Interfaces;

namespace MultiTenantIdentityApi.API.Controllers;

/// <summary>
/// Export controller for generating Excel reports
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ExportController : ControllerBase
{
    private readonly IExcelExportService _excelExportService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        IExcelExportService excelExportService,
        ITenantService tenantService,
        ILogger<ExportController> logger)
    {
        _excelExportService = excelExportService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Export tenants to Excel
    /// </summary>
    [HttpGet("tenants")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportTenants()
    {
        var tenantsResult = await _tenantService.GetAllTenantsAsync();

        if (!tenantsResult.Succeeded || tenantsResult.Data == null)
        {
            return tenantsResult.ToActionResult();
        }

        var excelResult = await _excelExportService.ExportToExcelAsync(
            tenantsResult.Data,
            sheetName: "Tenants");

        if (!excelResult.Succeeded || excelResult.Data == null)
        {
            return excelResult.ToActionResult();
        }

        return File(
            excelResult.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Tenants_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    /// <summary>
    /// Generic export endpoint - accepts JSON data and exports to Excel
    /// </summary>
    [HttpPost("generic")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportGeneric(
        [FromBody] GenericExportRequest request)
    {
        if (request.Data == null || !request.Data.Any())
        {
            return BadRequest(new
            {
                succeeded = false,
                errors = new[] { "No data provided for export" }
            });
        }

        var excelResult = await _excelExportService.ExportMultipleSheetsAsync(
            new Dictionary<string, object>
            {
                { request.SheetName ?? "Data", request.Data }
            });

        if (!excelResult.Succeeded || excelResult.Data == null)
        {
            return excelResult.ToActionResult();
        }

        return File(
            excelResult.Data,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{request.FileName ?? "Export"}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }
}

/// <summary>
/// Request model for generic export
/// </summary>
public record GenericExportRequest
{
    public List<Dictionary<string, object>> Data { get; init; } = new();
    public string? SheetName { get; init; }
    public string? FileName { get; init; }
}
