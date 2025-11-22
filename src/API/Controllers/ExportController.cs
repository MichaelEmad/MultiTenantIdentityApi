using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> ExportTenants()
    {
        try
        {
            var tenantsResult = await _tenantService.GetAllTenantsAsync();

            if (tenantsResult.Data == null)
            {
                return BadRequest(new { error = "Failed to retrieve tenants" });
            }

            var excelData = await _excelExportService.ExportToExcelAsync(
                tenantsResult.Data,
                sheetName: "Tenants");

            return File(
                excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Tenants_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tenants to Excel");
            return StatusCode(500, new { error = "An error occurred while exporting data" });
        }
    }

    /// <summary>
    /// Generic export endpoint - accepts JSON data and exports to Excel
    /// </summary>
    [HttpPost("generic")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportGeneric(
        [FromBody] GenericExportRequest request)
    {
        try
        {
            if (request.Data == null || !request.Data.Any())
            {
                return BadRequest(new { error = "No data provided for export" });
            }

            var excelData = await _excelExportService.ExportMultipleSheetsAsync(
                new Dictionary<string, object>
                {
                    { request.SheetName ?? "Data", request.Data }
                });

            return File(
                excelData,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{request.FileName ?? "Export"}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting generic data to Excel");
            return StatusCode(500, new { error = "An error occurred while exporting data" });
        }
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
