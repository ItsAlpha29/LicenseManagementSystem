using System.Security.Claims;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,IT_MANAGER")]
public class RenewalsController : ControllerBase
{
    private readonly IRenewalService _renewalService;

    public RenewalsController(IRenewalService renewalService)
    {
        _renewalService = renewalService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<RenewalDto>>>> GetRenewals(
        [FromQuery] string? search,
        [FromQuery] int? licenseId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _renewalService.GetRenewalsAsync(search, licenseId, status, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<RenewalDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<RenewalDto>>> GetRenewalById(int id)
    {
        var result = await _renewalService.GetRenewalByIdAsync(id);
        return Ok(ApiResponse<RenewalDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RenewalDto>>> ProcessRenewal([FromBody] CreateRenewalDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _renewalService.ProcessRenewalAsync(request, userId);
        return CreatedAtAction(nameof(GetRenewalById), new { id = result.Id }, ApiResponse<RenewalDto>.Ok(result, "License renewed successfully."));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
