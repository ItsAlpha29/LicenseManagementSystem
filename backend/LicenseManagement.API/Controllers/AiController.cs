using System.Security.Claims;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<AiChatResponseDto>>> Chat([FromBody] AiChatRequestDto request)
    {
        var userId = GetCurrentUserId();
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "EMPLOYEE";
        var empIdStr = User.FindFirstValue("EmployeeId");
        int? employeeId = int.TryParse(empIdStr, out var empId) ? empId : null;

        var response = await _aiService.ProcessChatAsync(request, userId, userRole, employeeId);
        return Ok(ApiResponse<AiChatResponseDto>.Ok(response));
    }

    [HttpPost("action/confirm")]
    [Authorize(Roles = "ADMIN,IT_MANAGER")]
    public async Task<ActionResult<ApiResponse<AiActionConfirmResponseDto>>> ConfirmAction([FromBody] AiActionConfirmRequestDto request)
    {
        var userId = GetCurrentUserId();
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "EMPLOYEE";

        var response = await _aiService.ConfirmActionAsync(request, userId, userRole);
        return Ok(ApiResponse<AiActionConfirmResponseDto>.Ok(response));
    }

    [HttpGet("recommendations/employee/{employeeId}")]
    public async Task<ActionResult<ApiResponse<SoftwareRecommendationResultDto>>> GetRecommendationsForEmployee(int employeeId)
    {
        var result = await _aiService.GetRecommendationsForEmployeeAsync(employeeId);
        return Ok(ApiResponse<SoftwareRecommendationResultDto>.Ok(result));
    }

    [HttpPost("recommendations/role")]
    public async Task<ActionResult<ApiResponse<SoftwareRecommendationResultDto>>> GetRecommendationsByRole([FromBody] SoftwareRecommendationRequestDto request)
    {
        var result = await _aiService.GetRecommendationsByRoleAsync(request.Department, request.Designation, request.EmployeeId);
        return Ok(ApiResponse<SoftwareRecommendationResultDto>.Ok(result));
    }

    [HttpPost("duplicate-check")]
    [Authorize(Roles = "ADMIN,IT_MANAGER")]
    public async Task<ActionResult<ApiResponse<DuplicateDetectionResultDto>>> CheckDuplicateLicense([FromBody] DuplicateDetectionRequestDto request)
    {
        var result = await _aiService.CheckDuplicateLicenseAsync(request);
        return Ok(ApiResponse<DuplicateDetectionResultDto>.Ok(result));
    }

    [HttpGet("redundancy-scan")]
    [Authorize(Roles = "ADMIN,IT_MANAGER")]
    public async Task<ActionResult<ApiResponse<PortfolioRedundancyScanResultDto>>> ScanPortfolioRedundancy()
    {
        var result = await _aiService.ScanPortfolioRedundancyAsync();
        return Ok(ApiResponse<PortfolioRedundancyScanResultDto>.Ok(result));
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
