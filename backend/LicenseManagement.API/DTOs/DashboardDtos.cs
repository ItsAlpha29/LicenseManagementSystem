namespace LicenseManagement.API.DTOs;

public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int TotalLicenses { get; set; }
    public int AssignedLicenses { get; set; }
    public int AvailableLicenses { get; set; }
    public double OverallUtilizationPercentage { get; set; }
    public int ExpiringSoonLicenses { get; set; }
    public int ExpiredLicenses { get; set; }
    public int PendingRenewals { get; set; }
    public decimal TotalLicenseCost { get; set; }
    public decimal TotalRenewalCost { get; set; }
}

public class LicenseUtilizationChartDto
{
    public string SoftwareName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Assigned { get; set; }
    public int Available { get; set; }
    public double UtilizationRate { get; set; }
}

public class StatusDistributionDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class DepartmentUsageDto
{
    public string Department { get; set; } = string.Empty;
    public int LicenseCount { get; set; }
    public int EmployeeCount { get; set; }
    public decimal TotalCost { get; set; }
}

public class MonthlyCostTrendDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal RenewalCost { get; set; }
}

public class ExpiryTimelineDto
{
    public int Expired { get; set; }
    public int CriticalWithin7Days { get; set; }
    public int ExpiringWithin30Days { get; set; }
    public int UpcomingWithin90Days { get; set; }
    public int ActiveBeyond90Days { get; set; }
}

public class DashboardFullResponseDto
{
    public DashboardSummaryDto Summary { get; set; } = new();
    public List<LicenseUtilizationChartDto> Utilization { get; set; } = new();
    public List<StatusDistributionDto> StatusDistribution { get; set; } = new();
    public List<DepartmentUsageDto> DepartmentUsage { get; set; } = new();
    public List<MonthlyCostTrendDto> MonthlyCostTrend { get; set; } = new();
    public ExpiryTimelineDto ExpiryTimeline { get; set; } = new();
    public List<LicenseDto> TopExpiringLicenses { get; set; } = new();
    public List<LicenseDto> UnderutilizedLicenses { get; set; } = new();
    public List<RenewalDto> UpcomingRenewals { get; set; } = new();
    public List<AuditHistoryDto> RecentActivities { get; set; } = new();
    public List<AiInsightItemDto> AiInsights { get; set; } = new();
}

public class AiInsightItemDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "OPTIMIZATION"; // OPTIMIZATION, RISK, ALERT, CAPACITY
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Severity { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
}
