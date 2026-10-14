namespace LicenseManagement.API.DTOs;

public class ReportFilterDto
{
    public string? ReportType { get; set; } // Utilization, Expiry, Employee, Cost, Renewal, Department
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Department { get; set; }
    public int? LicenseId { get; set; }
    public string? Status { get; set; }
    public string? Vendor { get; set; }
    public string? SearchTerm { get; set; }
}

public class LicenseUtilizationReportItemDto
{
    public string LicenseCode { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public int TotalLicenses { get; set; }
    public int AssignedCount { get; set; }
    public int AvailableCount { get; set; }
    public double UtilizationRate { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ExpiryReportItemDto
{
    public string LicenseCode { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public int AssignedCount { get; set; }
    public decimal RenewalCost { get; set; }
    public string UrgencyLevel { get; set; } = string.Empty; // Expired, Critical (<7d), Expiring Soon (<30d), Upcoming (<90d), Active
    public string Status { get; set; } = string.Empty;
}

public class EmployeeLicenseReportItemDto
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string LicenseCode { get; set; } = string.Empty;
    public DateTime AssignDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CostReportItemDto
{
    public string Category { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public int TotalLicenses { get; set; }
    public decimal UnitCost { get; set; }
    public decimal AnnualCost { get; set; }
    public decimal RenewalCost { get; set; }
    public string DepartmentAttribution { get; set; } = string.Empty;
}

public class RenewalReportItemDto
{
    public int RenewalId { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public DateTime RenewalDate { get; set; }
    public decimal RenewalAmount { get; set; }
    public DateTime PreviousExpiryDate { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public string RenewedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class DepartmentReportItemDto
{
    public string Department { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int TotalLicensesAssigned { get; set; }
    public int UniqueSoftwareCount { get; set; }
    public decimal TotalCostAttributed { get; set; }
    public List<string> TopSoftwareAssigned { get; set; } = new();
}
