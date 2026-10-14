using System.Security.Claims;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LicensesController : ControllerBase
{
    private readonly ILicenseService _licenseService;

    public LicensesController(ILicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<LicenseDto>>>> GetLicenses(
        [FromQuery] string? search,
        [FromQuery] string? licenseType,
        [FromQuery] string? vendor,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "software",
        [FromQuery] bool sortDesc = false)
    {
        var result = await _licenseService.GetLicensesAsync(search, licenseType, vendor, status, pageNumber, pageSize, sortBy, sortDesc);
        return Ok(ApiResponse<PagedResult<LicenseDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<LicenseDetailDto>>> GetLicenseById(int id)
    {
        var result = await _licenseService.GetLicenseByIdAsync(id);
        return Ok(ApiResponse<LicenseDetailDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,IT_MANAGER")]
    public async Task<ActionResult<ApiResponse<LicenseDto>>> CreateLicense([FromBody] CreateLicenseDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _licenseService.CreateLicenseAsync(request, userId);
        return CreatedAtAction(nameof(GetLicenseById), new { id = result.Id }, ApiResponse<LicenseDto>.Ok(result, "License created successfully."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN,IT_MANAGER")]
    public async Task<ActionResult<ApiResponse<LicenseDto>>> UpdateLicense(int id, [FromBody] UpdateLicenseDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _licenseService.UpdateLicenseAsync(id, request, userId);
        return Ok(ApiResponse<LicenseDto>.Ok(result, "License updated successfully."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")] // Rule 11: Only ADMIN can delete licenses
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLicense(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _licenseService.DeleteLicenseAsync(id, userId);
        return Ok(ApiResponse<bool>.Ok(result, "License deleted successfully."));
    }

    [HttpGet("vendors")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetVendors()
    {
        var result = await _licenseService.GetVendorsAsync();
        return Ok(ApiResponse<List<string>>.Ok(result));
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetLicenseTypes()
    {
        var result = await _licenseService.GetLicenseTypesAsync();
        return Ok(ApiResponse<List<string>>.Ok(result));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
