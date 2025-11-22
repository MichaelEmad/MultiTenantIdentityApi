using Microsoft.AspNetCore.Mvc;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Application.DTOs.Tenants;

namespace MultiTenantIdentityApi.API.Controllers;

/// <summary>
/// Tenant management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(ITenantService tenantService, ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Get all tenants
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _tenantService.GetAllTenantsAsync();
        return Ok(result.Data);
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _tenantService.GetTenantByIdAsync(id);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get tenant by identifier
    /// </summary>
    [HttpGet("by-identifier/{identifier}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdentifier(string identifier)
    {
        var result = await _tenantService.GetTenantByIdentifierAsync(identifier);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
    {
        var result = await _tenantService.CreateTenantAsync(request);

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Update a tenant
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTenantRequest request)
    {
        var result = await _tenantService.UpdateTenantAsync(id, request);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a tenant
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _tenantService.DeleteTenantAsync(id);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Activate a tenant
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(string id)
    {
        var result = await _tenantService.ActivateTenantAsync(id);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Deactivate a tenant
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id)
    {
        var result = await _tenantService.DeactivateTenantAsync(id);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}
