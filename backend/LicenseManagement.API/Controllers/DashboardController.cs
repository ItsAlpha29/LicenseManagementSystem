using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("all")]
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DashboardFullResponseDto>>> GetFullDashboard()
    {
        var result = await _dashboardService.GetDashboardDataAsync();
        return Ok(ApiResponse<DashboardFullResponseDto>.Ok(result));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary()
    {
        var result = await _dashboardService.GetSummaryMetricsAsync();
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(result));
    }

    [HttpGet("license-utilization")]
    public async Task<ActionResult<ApiResponse<List<LicenseUtilizationChartDto>>>> GetLicenseUtilization()
    {
        var result = await _dashboardService.GetUtilizationDataAsync();
        return Ok(ApiResponse<List<LicenseUtilizationChartDto>>.Ok(result));
    }

    [HttpGet("cost")]
    public async Task<ActionResult<ApiResponse<List<MonthlyCostTrendDto>>>> GetCostTrend()
    {
        var result = await _dashboardService.GetCostTrendAsync();
        return Ok(ApiResponse<List<MonthlyCostTrendDto>>.Ok(result));
    }

    [HttpGet("expiry")]
    public async Task<ActionResult<ApiResponse<ExpiryTimelineDto>>> GetExpiryTimeline()
    {
        var result = await _dashboardService.GetExpiryTimelineAsync();
        return Ok(ApiResponse<ExpiryTimelineDto>.Ok(result));
    }

    [HttpGet("recent-activities")]
    public async Task<ActionResult<ApiResponse<List<AuditHistoryDto>>>> GetRecentActivities([FromQuery] int limit = 10)
    {
        var result = await _dashboardService.GetRecentActivitiesAsync(limit);
        return Ok(ApiResponse<List<AuditHistoryDto>>.Ok(result));
    }
}
