using System.Security.Claims;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LicenseManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN,IT_MANAGER")]
    public async Task<ActionResult<ApiResponse<PagedResult<EmployeeDto>>>> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] string? department,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool sortDesc = false)
    {
        var result = await _employeeService.GetEmployeesAsync(search, department, role, status, pageNumber, pageSize, sortBy, sortDesc);
        return Ok(ApiResponse<PagedResult<EmployeeDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "ADMIN,IT_MANAGER")]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> GetEmployeeById(int id)
    {
        var result = await _employeeService.GetEmployeeByIdAsync(id);
        return Ok(ApiResponse<EmployeeDetailDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> CreateEmployee([FromBody] CreateEmployeeDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _employeeService.CreateEmployeeAsync(request, userId);
        return CreatedAtAction(nameof(GetEmployeeById), new { id = result.Id }, ApiResponse<EmployeeDto>.Ok(result, "Employee created successfully."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _employeeService.UpdateEmployeeAsync(id, request, userId);
        return Ok(ApiResponse<EmployeeDto>.Ok(result, "Employee updated successfully."));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<EmployeeDto>>> UpdateStatus(int id, [FromBody] UpdateEmployeeStatusDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _employeeService.UpdateStatusAsync(id, request.Status, userId);
        return Ok(ApiResponse<EmployeeDto>.Ok(result, $"Employee status updated to {request.Status}."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteEmployee(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _employeeService.DeleteEmployeeAsync(id, userId);
        return Ok(ApiResponse<bool>.Ok(result, "Employee deleted successfully."));
    }

    [HttpGet("departments")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetDepartments()
    {
        var result = await _employeeService.GetDepartmentsAsync();
        return Ok(ApiResponse<List<string>>.Ok(result));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }
}
