namespace LicenseManagement.API.DTOs;

public class AssignmentResponseDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeEmail { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int LicenseId { get; set; }
    public string LicenseCode { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public DateTime AssignDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining => (int)Math.Ceiling((ExpiryDate.Date - DateTime.UtcNow.Date).TotalDays);
    public int? AssignedByUserId { get; set; }
    public string? AssignedByUserName { get; set; }
    public DateTime? RevokedDate { get; set; }
    public int? RevokedByUserId { get; set; }
    public string? RevokedByUserName { get; set; }
    public string? RevocationReason { get; set; }
    public string Status { get; set; } = "Active"; // Active, Revoked, Expired
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignLicenseDto
{
    public int EmployeeId { get; set; }
    public int LicenseId { get; set; }
    public DateTime? ExpiryDate { get; set; } // Optional, defaults to license ExpiryDate
    public string? Remarks { get; set; }
}

public class RevokeLicenseDto
{
    public string RevocationReason { get; set; } = string.Empty; // Employee Left, No Longer Required, License Reallocation, Other
    public string? Remarks { get; set; }
}
