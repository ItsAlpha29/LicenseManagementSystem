using System.Security.Claims;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,IT_MANAGER")]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;

    public AssignmentsController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AssignmentResponseDto>>>> GetAssignments(
        [FromQuery] string? search,
        [FromQuery] string? department,
        [FromQuery] string? status,
        [FromQuery] int? employeeId,
        [FromQuery] int? licenseId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _assignmentService.GetAssignmentsAsync(search, department, status, employeeId, licenseId, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<AssignmentResponseDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AssignmentResponseDto>>> GetAssignmentById(int id)
    {
        var result = await _assignmentService.GetAssignmentByIdAsync(id);
        return Ok(ApiResponse<AssignmentResponseDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssignmentResponseDto>>> AssignLicense([FromBody] AssignLicenseDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _assignmentService.AssignLicenseAsync(request, userId);
        return CreatedAtAction(nameof(GetAssignmentById), new { id = result.Id }, ApiResponse<AssignmentResponseDto>.Ok(result, "License successfully assigned."));
    }

    [HttpPost("{id:int}/revoke")]
    public async Task<ActionResult<ApiResponse<AssignmentResponseDto>>> RevokeLicense(int id, [FromBody] RevokeLicenseDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _assignmentService.RevokeLicenseAsync(id, request, userId);
        return Ok(ApiResponse<AssignmentResponseDto>.Ok(result, "License successfully revoked."));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
