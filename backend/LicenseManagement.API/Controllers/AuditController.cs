using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")] // Enterprise security: Audit log access is reserved for ADMIN
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditHistoryDto>>>> GetAuditLogs(
        [FromQuery] string? module,
        [FromQuery] string? action,
        [FromQuery] int? userId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _auditService.GetAuditLogsAsync(module, action, userId, startDate, endDate, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<AuditHistoryDto>>.Ok(result));
    }
}
