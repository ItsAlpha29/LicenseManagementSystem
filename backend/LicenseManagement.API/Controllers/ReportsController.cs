using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN,IT_MANAGER")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("license-utilization")]
    public async Task<ActionResult<ApiResponse<List<LicenseUtilizationReportItemDto>>>> GetLicenseUtilization([FromQuery] ReportFilterDto filter)
    {
        var result = await _reportService.GetLicenseUtilizationReportAsync(filter);
        return Ok(ApiResponse<List<LicenseUtilizationReportItemDto>>.Ok(result));
    }

    [HttpGet("expiry")]
    public async Task<ActionResult<ApiResponse<List<ExpiryReportItemDto>>>> GetExpiry([FromQuery] ReportFilterDto filter)
    {
        var result = await _reportService.GetExpiryReportAsync(filter);
        return Ok(ApiResponse<List<ExpiryReportItemDto>>.Ok(result));
    }

    [HttpGet("employee-licenses")]
    public async Task<ActionResult<ApiResponse<List<EmployeeLicenseReportItemDto>>>> GetEmployeeLicenses([FromQuery] ReportFilterDto filter)
    {
        var result = await _reportService.GetEmployeeLicenseReportAsync(filter);
        return Ok(ApiResponse<List<EmployeeLicenseReportItemDto>>.Ok(result));
    }

    [HttpGet("cost")]
    public async Task<ActionResult<ApiResponse<List<CostReportItemDto>>>> GetCost([FromQuery] ReportFilterDto filter)
    {
        var result = await _reportService.GetCostReportAsync(filter);
        return Ok(ApiResponse<List<CostReportItemDto>>.Ok(result));
    }

    [HttpGet("renewals")]
    public async Task<ActionResult<ApiResponse<List<RenewalReportItemDto>>>> GetRenewals([FromQuery] ReportFilterDto filter)
    {
        var result = await _reportService.GetRenewalReportAsync(filter);
        return Ok(ApiResponse<List<RenewalReportItemDto>>.Ok(result));
    }

    [HttpGet("department")]
    public async Task<ActionResult<ApiResponse<List<DepartmentReportItemDto>>>> GetDepartment([FromQuery] ReportFilterDto filter)
    {
        var result = await _reportService.GetDepartmentReportAsync(filter);
        return Ok(ApiResponse<List<DepartmentReportItemDto>>.Ok(result));
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string reportType, [FromQuery] ReportFilterDto filter)
    {
        var csvBytes = await _reportService.ExportReportCsvAsync(reportType, filter);
        var fileName = $"{reportType.ToLower()}-report-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return File(csvBytes, "text/csv", fileName);
    }
}
