using System.Security.Claims;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/my/licenses")]
[Authorize]
public class MyLicensesController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;

    public MyLicensesController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AssignmentResponseDto>>>> GetMyLicenses()
    {
        var employeeIdClaim = User.FindFirstValue("EmployeeId");
        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out var employeeId) || employeeId <= 0)
        {
            return Ok(ApiResponse<List<AssignmentResponseDto>>.Ok(new List<AssignmentResponseDto>(), "No employee profile linked to account."));
        }

        var result = await _assignmentService.GetMyLicensesAsync(employeeId);
        return Ok(ApiResponse<List<AssignmentResponseDto>>.Ok(result));
    }
}
