namespace LicenseManagement.API.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public DateTime JoiningDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ActiveLicensesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEmployeeDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Role { get; set; } = "EMPLOYEE";
    public int? ManagerId { get; set; }
    public DateTime JoiningDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";
    public string? InitialPassword { get; set; }
}

public class UpdateEmployeeDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Role { get; set; } = "EMPLOYEE";
    public int? ManagerId { get; set; }
    public DateTime JoiningDate { get; set; }
    public string Status { get; set; } = "Active";
}

public class UpdateEmployeeStatusDto
{
    public string Status { get; set; } = "Active"; // Active, Inactive, OnLeave
}

public class EmployeeDetailDto : EmployeeDto
{
    public List<AssignmentResponseDto> AssignedLicenses { get; set; } = new();
    public List<AuditHistoryDto> ActivityLogs { get; set; } = new();
}
