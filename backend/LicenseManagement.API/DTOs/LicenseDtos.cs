namespace LicenseManagement.API.DTOs;

public class LicenseDto
{
    public int Id { get; set; }
    public string LicenseCode { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int TotalLicenses { get; set; }
    public int AssignedCount { get; set; }
    public int AvailableCount { get; set; }
    public double UtilizationPercentage => TotalLicenses > 0 ? Math.Round((double)AssignedCount / TotalLicenses * 100, 1) : 0;
    public decimal Cost { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public decimal RenewalCost { get; set; }
    public string? LicenseReference { get; set; }
    public string Status { get; set; } = string.Empty; // Active, ExpiringSoon, Expired, Deprecated
    public string? Notes { get; set; }
    public int DaysRemaining => (int)Math.Ceiling((ExpiryDate.Date - DateTime.UtcNow.Date).TotalDays);
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLicenseDto
{
    public string LicenseCode { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string LicenseType { get; set; } = "Subscription";
    public string Vendor { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddYears(1);
    public int TotalLicenses { get; set; }
    public decimal Cost { get; set; }
    public string BillingCycle { get; set; } = "Annual";
    public decimal RenewalCost { get; set; }
    public string? LicenseReference { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
}

public class UpdateLicenseDto
{
    public string SoftwareName { get; set; } = string.Empty;
    public string LicenseType { get; set; } = "Subscription";
    public string Vendor { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int TotalLicenses { get; set; }
    public decimal Cost { get; set; }
    public string BillingCycle { get; set; } = "Annual";
    public decimal RenewalCost { get; set; }
    public string? LicenseReference { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
}

public class LicenseDetailDto : LicenseDto
{
    public List<AssignmentResponseDto> CurrentAssignments { get; set; } = new();
    public List<RenewalDto> RenewalHistory { get; set; } = new();
}
