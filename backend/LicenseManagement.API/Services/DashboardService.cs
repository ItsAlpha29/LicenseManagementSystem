using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface IDashboardService
{
    Task<DashboardFullResponseDto> GetDashboardDataAsync();
    Task<DashboardSummaryDto> GetSummaryMetricsAsync();
    Task<List<LicenseUtilizationChartDto>> GetUtilizationDataAsync();
    Task<List<MonthlyCostTrendDto>> GetCostTrendAsync();
    Task<ExpiryTimelineDto> GetExpiryTimelineAsync();
    Task<List<AuditHistoryDto>> GetRecentActivitiesAsync(int limit = 10);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardFullResponseDto> GetDashboardDataAsync()
    {
        var now = DateTime.UtcNow.Date;
        var in7Days = now.AddDays(7);
        var in30Days = now.AddDays(30);
        var in90Days = now.AddDays(90);

        var totalEmployees = await _context.Employees.CountAsync();
        var activeEmployees = await _context.Employees.CountAsync(e => e.Status == "Active");

        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();

        var totalLicensesCount = licenses.Sum(l => l.TotalLicenses);
        var assignedLicensesCount = licenses.Sum(l => l.AssignedCount);
        var availableLicensesCount = licenses.Sum(l => l.AvailableCount);
        var overallUtilization = totalLicensesCount > 0 ? Math.Round((double)assignedLicensesCount / totalLicensesCount * 100, 1) : 0;

        var expiringSoonCount = licenses.Count(l => l.ExpiryDate >= now && l.ExpiryDate <= in30Days);
        var expiredCount = licenses.Count(l => l.ExpiryDate < now);
        var totalCost = licenses.Sum(l => l.Cost);
        var totalRenewalCost = licenses.Sum(l => l.RenewalCost);

        var summary = new DashboardSummaryDto
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            TotalLicenses = totalLicensesCount,
            AssignedLicenses = assignedLicensesCount,
            AvailableLicenses = availableLicensesCount,
            OverallUtilizationPercentage = overallUtilization,
            ExpiringSoonLicenses = expiringSoonCount,
            ExpiredLicenses = expiredCount,
            PendingRenewals = expiringSoonCount + expiredCount,
            TotalLicenseCost = totalCost,
            TotalRenewalCost = totalRenewalCost
        };

        var utilization = licenses.Select(l => new LicenseUtilizationChartDto
        {
            SoftwareName = l.SoftwareName,
            Total = l.TotalLicenses,
            Assigned = l.AssignedCount,
            Available = l.AvailableCount,
            UtilizationRate = l.TotalLicenses > 0 ? Math.Round((double)l.AssignedCount / l.TotalLicenses * 100, 1) : 0
        }).OrderByDescending(u => u.Total).Take(8).ToList();

        var activeCount = licenses.Count(l => l.ExpiryDate > in30Days);
        var statusDistribution = new List<StatusDistributionDto>
        {
            new() { Name = "Active", Value = activeCount, Color = "#10b981" },
            new() { Name = "Expiring Soon", Value = expiringSoonCount, Color = "#f59e0b" },
            new() { Name = "Expired", Value = expiredCount, Color = "#ef4444" }
        };

        // Department-wise usage
        var assignments = await _context.LicenseAssignments
            .Include(a => a.Employee)
            .Include(a => a.License)
            .Where(a => a.Status == "Active")
            .AsNoTracking()
            .ToListAsync();

        var departmentUsage = assignments
            .Where(a => a.Employee != null && a.License != null)
            .GroupBy(a => a.Employee!.Department ?? "Unassigned")
            .Select(g => new DepartmentUsageDto
            {
                Department = g.Key,
                LicenseCount = g.Count(),
                EmployeeCount = g.Select(x => x.EmployeeId).Distinct().Count(),
                TotalCost = g.Sum(x => x.License!.TotalLicenses > 0 ? x.License.Cost / x.License.TotalLicenses : 0)
            })
            .OrderByDescending(d => d.LicenseCount)
            .ToList();

        // Monthly Cost Trend (Simulated 6 months of historical and projected based on current billing)
        var months = new[] { "Mar", "Apr", "May", "Jun", "Jul", "Aug" };
        var monthlyTrend = months.Select((m, i) => new MonthlyCostTrendDto
        {
            Month = m,
            Cost = Math.Round(totalCost * (0.85m + (i * 0.03m)), 2),
            RenewalCost = Math.Round(totalRenewalCost * (0.15m + (i * 0.05m)), 2)
        }).ToList();

        // Expiry timeline
        var expiryTimeline = new ExpiryTimelineDto
        {
            Expired = licenses.Count(l => l.ExpiryDate < now),
            CriticalWithin7Days = licenses.Count(l => l.ExpiryDate >= now && l.ExpiryDate <= in7Days),
            ExpiringWithin30Days = licenses.Count(l => l.ExpiryDate > in7Days && l.ExpiryDate <= in30Days),
            UpcomingWithin90Days = licenses.Count(l => l.ExpiryDate > in30Days && l.ExpiryDate <= in90Days),
            ActiveBeyond90Days = licenses.Count(l => l.ExpiryDate > in90Days)
        };

        // Top Expiring Licenses
        var topExpiring = licenses
            .Where(l => l.ExpiryDate >= now)
            .OrderBy(l => l.ExpiryDate)
            .Take(5)
            .Select(l => new LicenseDto
            {
                Id = l.Id,
                LicenseCode = l.LicenseCode,
                SoftwareName = l.SoftwareName,
                LicenseType = l.LicenseType,
                Vendor = l.Vendor,
                ExpiryDate = l.ExpiryDate,
                TotalLicenses = l.TotalLicenses,
                AssignedCount = l.AssignedCount,
                AvailableCount = l.AvailableCount,
                Cost = l.Cost,
                Status = l.Status
            }).ToList();

        // Under-utilized Licenses (<50% utilization)
        var underutilized = licenses
            .Where(l => l.TotalLicenses >= 5 && ((double)l.AssignedCount / l.TotalLicenses) < 0.6)
            .OrderBy(l => (double)l.AssignedCount / l.TotalLicenses)
            .Take(4)
            .Select(l => new LicenseDto
            {
                Id = l.Id,
                LicenseCode = l.LicenseCode,
                SoftwareName = l.SoftwareName,
                LicenseType = l.LicenseType,
                Vendor = l.Vendor,
                ExpiryDate = l.ExpiryDate,
                TotalLicenses = l.TotalLicenses,
                AssignedCount = l.AssignedCount,
                AvailableCount = l.AvailableCount,
                Cost = l.Cost,
                Status = l.Status
            }).ToList();

        // Upcoming Renewals
        var upcomingRenewals = await _context.Renewals
            .Include(r => r.License)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new RenewalDto
            {
                Id = r.Id,
                LicenseId = r.LicenseId,
                LicenseCode = r.License.LicenseCode,
                SoftwareName = r.License.SoftwareName,
                Vendor = r.License.Vendor,
                RenewalDate = r.RenewalDate,
                RenewalAmount = r.RenewalAmount,
                NewExpiryDate = r.NewExpiryDate,
                Status = r.Status
            }).ToListAsync();

        // Recent activities
        var recentActivities = await _context.AuditHistories
            .Include(a => a.User)
            .ThenInclude(u => u!.Employee)
            .OrderByDescending(a => a.DateTime)
            .Take(8)
            .Select(a => new AuditHistoryDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? (a.User.Employee != null ? a.User.Employee.EmployeeName : a.User.Email) : "System",
                UserRole = a.User != null ? a.User.Role : "SYSTEM",
                Action = a.Action,
                Module = a.Module,
                DateTime = a.DateTime,
                Remarks = a.Remarks
            }).ToListAsync();

        // Dynamic AI Insights generated from actual database metrics
        var insights = GenerateAiInsights(licenses, departmentUsage, overallUtilization);

        return new DashboardFullResponseDto
        {
            Summary = summary,
            Utilization = utilization,
            StatusDistribution = statusDistribution,
            DepartmentUsage = departmentUsage,
            MonthlyCostTrend = monthlyTrend,
            ExpiryTimeline = expiryTimeline,
            TopExpiringLicenses = topExpiring,
            UnderutilizedLicenses = underutilized,
            UpcomingRenewals = upcomingRenewals,
            RecentActivities = recentActivities,
            AiInsights = insights
        };
    }

    public async Task<DashboardSummaryDto> GetSummaryMetricsAsync()
    {
        var now = DateTime.UtcNow.Date;
        var in30Days = now.AddDays(30);

        var totalEmployees = await _context.Employees.CountAsync();
        var activeEmployees = await _context.Employees.CountAsync(e => e.Status == "Active");
        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();

        var totalLicensesCount = licenses.Sum(l => l.TotalLicenses);
        var assignedLicensesCount = licenses.Sum(l => l.AssignedCount);
        var availableLicensesCount = licenses.Sum(l => l.AvailableCount);

        return new DashboardSummaryDto
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            TotalLicenses = totalLicensesCount,
            AssignedLicenses = assignedLicensesCount,
            AvailableLicenses = availableLicensesCount,
            OverallUtilizationPercentage = totalLicensesCount > 0 ? Math.Round((double)assignedLicensesCount / totalLicensesCount * 100, 1) : 0,
            ExpiringSoonLicenses = licenses.Count(l => l.ExpiryDate >= now && l.ExpiryDate <= in30Days),
            ExpiredLicenses = licenses.Count(l => l.ExpiryDate < now),
            PendingRenewals = licenses.Count(l => l.ExpiryDate <= in30Days),
            TotalLicenseCost = licenses.Sum(l => l.Cost),
            TotalRenewalCost = licenses.Sum(l => l.RenewalCost)
        };
    }

    public async Task<List<LicenseUtilizationChartDto>> GetUtilizationDataAsync()
    {
        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();
        return licenses.Select(l => new LicenseUtilizationChartDto
        {
            SoftwareName = l.SoftwareName,
            Total = l.TotalLicenses,
            Assigned = l.AssignedCount,
            Available = l.AvailableCount,
            UtilizationRate = l.TotalLicenses > 0 ? Math.Round((double)l.AssignedCount / l.TotalLicenses * 100, 1) : 0
        }).OrderByDescending(u => u.Total).ToList();
    }

    public async Task<List<MonthlyCostTrendDto>> GetCostTrendAsync()
    {
        var totalCost = await _context.Licenses.SumAsync(l => l.Cost);
        var totalRenewalCost = await _context.Licenses.SumAsync(l => l.RenewalCost);
        var months = new[] { "Mar", "Apr", "May", "Jun", "Jul", "Aug" };
        return months.Select((m, i) => new MonthlyCostTrendDto
        {
            Month = m,
            Cost = Math.Round(totalCost * (0.85m + (i * 0.03m)), 2),
            RenewalCost = Math.Round(totalRenewalCost * (0.15m + (i * 0.05m)), 2)
        }).ToList();
    }

    public async Task<ExpiryTimelineDto> GetExpiryTimelineAsync()
    {
        var now = DateTime.UtcNow.Date;
        var in7Days = now.AddDays(7);
        var in30Days = now.AddDays(30);
        var in90Days = now.AddDays(90);

        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();

        return new ExpiryTimelineDto
        {
            Expired = licenses.Count(l => l.ExpiryDate < now),
            CriticalWithin7Days = licenses.Count(l => l.ExpiryDate >= now && l.ExpiryDate <= in7Days),
            ExpiringWithin30Days = licenses.Count(l => l.ExpiryDate > in7Days && l.ExpiryDate <= in30Days),
            UpcomingWithin90Days = licenses.Count(l => l.ExpiryDate > in30Days && l.ExpiryDate <= in90Days),
            ActiveBeyond90Days = licenses.Count(l => l.ExpiryDate > in90Days)
        };
    }

    public async Task<List<AuditHistoryDto>> GetRecentActivitiesAsync(int limit = 10)
    {
        return await _context.AuditHistories
            .Include(a => a.User)
            .ThenInclude(u => u!.Employee)
            .OrderByDescending(a => a.DateTime)
            .Take(limit)
            .Select(a => new AuditHistoryDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? (a.User.Employee != null ? a.User.Employee.EmployeeName : a.User.Email) : "System",
                UserRole = a.User != null ? a.User.Role : "SYSTEM",
                Action = a.Action,
                Module = a.Module,
                DateTime = a.DateTime,
                Remarks = a.Remarks
            }).ToListAsync();
    }

    private static List<AiInsightItemDto> GenerateAiInsights(List<LicenseMaster> licenses, List<DepartmentUsageDto> departments, double overallUtilization)
    {
        var insights = new List<AiInsightItemDto>();
        var now = DateTime.UtcNow.Date;

        // 1. High capacity pressure insight
        var nearCapacity = licenses.FirstOrDefault(l => l.TotalLicenses >= 10 && ((double)l.AssignedCount / l.TotalLicenses) >= 0.85);
        if (nearCapacity != null)
        {
            var util = Math.Round((double)nearCapacity.AssignedCount / nearCapacity.TotalLicenses * 100, 1);
            insights.Add(new AiInsightItemDto
            {
                Type = "CAPACITY",
                Title = $"High Utilization Alert: {nearCapacity.SoftwareName}",
                Description = $"{nearCapacity.SoftwareName} is currently {util}% utilized with only {nearCapacity.AvailableCount} available seats remaining.",
                Recommendation = "Consider provisioning additional seats before upcoming quarterly hiring cycles to prevent provisioning delays.",
                Severity = "HIGH",
                ActionText = "View License",
                ActionUrl = $"/licenses"
            });
        }

        // 2. Imminent renewal risk
        var expiringSoon = licenses.Where(l => l.ExpiryDate >= now && l.ExpiryDate <= now.AddDays(30)).OrderBy(l => l.ExpiryDate).FirstOrDefault();
        if (expiringSoon != null)
        {
            var days = (int)Math.Ceiling((expiringSoon.ExpiryDate - now).TotalDays);
            insights.Add(new AiInsightItemDto
            {
                Type = "RISK",
                Title = $"Renewal Required: {expiringSoon.SoftwareName}",
                Description = $"{expiringSoon.SoftwareName} expires in {days} day(s) on {expiringSoon.ExpiryDate:MMM dd, yyyy}. {expiringSoon.AssignedCount} active employees currently depend on this software.",
                Recommendation = $"Initiate renewal with vendor '{expiringSoon.Vendor}' for estimated renewal amount of ${expiringSoon.RenewalCost:N2}.",
                Severity = "CRITICAL",
                ActionText = "Renew Now",
                ActionUrl = "/renewals"
            });
        }

        // 3. Optimization/Cost saving opportunity
        var underutilized = licenses.FirstOrDefault(l => l.TotalLicenses >= 10 && ((double)l.AssignedCount / l.TotalLicenses) <= 0.4);
        if (underutilized != null)
        {
            var unassigned = underutilized.AvailableCount;
            var wastedCost = (underutilized.Cost / underutilized.TotalLicenses) * unassigned;
            insights.Add(new AiInsightItemDto
            {
                Type = "OPTIMIZATION",
                Title = $"Cost Optimization: {underutilized.SoftwareName}",
                Description = $"{underutilized.SoftwareName} has {unassigned} unassigned seats ({Math.Round((double)underutilized.AssignedCount / underutilized.TotalLicenses * 100)}% utilization), tying up approx. ${wastedCost:N2} in unused capacity.",
                Recommendation = "Downsize subscription tier on next renewal cycle or reallocate surplus licenses to other teams.",
                Severity = "MEDIUM",
                ActionText = "Optimize Seats",
                ActionUrl = "/licenses"
            });
        }

        // 4. Department distribution summary
        if (departments.Any())
        {
            var topDept = departments.First();
            insights.Add(new AiInsightItemDto
            {
                Type = "ALERT",
                Title = $"Department License Density: {topDept.Department}",
                Description = $"{topDept.Department} is the top software-consuming department with {topDept.LicenseCount} assigned licenses across {topDept.EmployeeCount} staff members.",
                Recommendation = "Audit quarterly department utilization to ensure all allocated toolsets are actively adopted.",
                Severity = "LOW",
                ActionText = "View Department Report",
                ActionUrl = "/reports"
            });
        }

        return insights;
    }
}
