using System.Text;
using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface IReportService
{
    Task<List<LicenseUtilizationReportItemDto>> GetLicenseUtilizationReportAsync(ReportFilterDto filter);
    Task<List<ExpiryReportItemDto>> GetExpiryReportAsync(ReportFilterDto filter);
    Task<List<EmployeeLicenseReportItemDto>> GetEmployeeLicenseReportAsync(ReportFilterDto filter);
    Task<List<CostReportItemDto>> GetCostReportAsync(ReportFilterDto filter);
    Task<List<RenewalReportItemDto>> GetRenewalReportAsync(ReportFilterDto filter);
    Task<List<DepartmentReportItemDto>> GetDepartmentReportAsync(ReportFilterDto filter);
    Task<byte[]> ExportReportCsvAsync(string reportType, ReportFilterDto filter);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LicenseUtilizationReportItemDto>> GetLicenseUtilizationReportAsync(ReportFilterDto filter)
    {
        var query = _context.Licenses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Vendor) && filter.Vendor != "All")
            query = query.Where(l => l.Vendor.ToLower() == filter.Vendor.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "All")
            query = query.Where(l => l.Status.ToLower() == filter.Status.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var s = filter.SearchTerm.ToLower();
            query = query.Where(l => l.SoftwareName.ToLower().Contains(s) || l.LicenseCode.ToLower().Contains(s));
        }

        var items = await query.ToListAsync();

        return items.Select(l => new LicenseUtilizationReportItemDto
        {
            LicenseCode = l.LicenseCode,
            SoftwareName = l.SoftwareName,
            Vendor = l.Vendor,
            LicenseType = l.LicenseType,
            TotalLicenses = l.TotalLicenses,
            AssignedCount = l.AssignedCount,
            AvailableCount = l.AvailableCount,
            UtilizationRate = l.TotalLicenses > 0 ? Math.Round((double)l.AssignedCount / l.TotalLicenses * 100, 1) : 0,
            UnitCost = l.TotalLicenses > 0 ? Math.Round(l.Cost / l.TotalLicenses, 2) : 0,
            TotalCost = l.Cost,
            Status = l.Status
        }).OrderByDescending(r => r.UtilizationRate).ToList();
    }

    public async Task<List<ExpiryReportItemDto>> GetExpiryReportAsync(ReportFilterDto filter)
    {
        var now = DateTime.UtcNow.Date;
        var query = _context.Licenses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Vendor) && filter.Vendor != "All")
            query = query.Where(l => l.Vendor.ToLower() == filter.Vendor.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "All")
            query = query.Where(l => l.Status.ToLower() == filter.Status.ToLower());

        if (filter.StartDate.HasValue)
            query = query.Where(l => l.ExpiryDate >= filter.StartDate.Value.ToUniversalTime());

        if (filter.EndDate.HasValue)
            query = query.Where(l => l.ExpiryDate <= filter.EndDate.Value.ToUniversalTime());

        var items = await query.ToListAsync();

        return items.Select(l =>
        {
            var daysRemaining = (int)Math.Ceiling((l.ExpiryDate.Date - now).TotalDays);
            var urgency = daysRemaining switch
            {
                < 0 => "Expired",
                <= 7 => "Critical (< 7d)",
                <= 30 => "Expiring Soon (< 30d)",
                <= 90 => "Upcoming (< 90d)",
                _ => "Active (> 90d)"
            };

            return new ExpiryReportItemDto
            {
                LicenseCode = l.LicenseCode,
                SoftwareName = l.SoftwareName,
                Vendor = l.Vendor,
                ExpiryDate = l.ExpiryDate,
                DaysRemaining = daysRemaining,
                AssignedCount = l.AssignedCount,
                RenewalCost = l.RenewalCost,
                UrgencyLevel = urgency,
                Status = l.Status
            };
        }).OrderBy(r => r.DaysRemaining).ToList();
    }

    public async Task<List<EmployeeLicenseReportItemDto>> GetEmployeeLicenseReportAsync(ReportFilterDto filter)
    {
        var query = _context.LicenseAssignments
            .Include(a => a.Employee)
            .Include(a => a.License)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Department) && filter.Department != "All")
            query = query.Where(a => a.Employee.Department.ToLower() == filter.Department.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.Status) && filter.Status != "All")
            query = query.Where(a => a.Status.ToLower() == filter.Status.ToLower());

        if (filter.LicenseId.HasValue && filter.LicenseId > 0)
            query = query.Where(a => a.LicenseId == filter.LicenseId.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var s = filter.SearchTerm.ToLower();
            query = query.Where(a =>
                a.Employee.EmployeeName.ToLower().Contains(s) ||
                a.Employee.EmployeeCode.ToLower().Contains(s) ||
                a.License.SoftwareName.ToLower().Contains(s));
        }

        var items = await query.ToListAsync();

        return items.Select(a => new EmployeeLicenseReportItemDto
        {
            EmployeeCode = a.Employee.EmployeeCode,
            EmployeeName = a.Employee.EmployeeName,
            Email = a.Employee.Email,
            Department = a.Employee.Department,
            Designation = a.Employee.Designation,
            SoftwareName = a.License.SoftwareName,
            LicenseCode = a.License.LicenseCode,
            AssignDate = a.AssignDate,
            ExpiryDate = a.ExpiryDate,
            Status = a.Status
        }).OrderBy(r => r.Department).ThenBy(r => r.EmployeeName).ToList();
    }

    public async Task<List<CostReportItemDto>> GetCostReportAsync(ReportFilterDto filter)
    {
        var query = _context.Licenses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Vendor) && filter.Vendor != "All")
            query = query.Where(l => l.Vendor.ToLower() == filter.Vendor.ToLower());

        var licenses = await query.ToListAsync();

        return licenses.Select(l => new CostReportItemDto
        {
            Category = l.LicenseType,
            SoftwareName = l.SoftwareName,
            Vendor = l.Vendor,
            BillingCycle = l.BillingCycle,
            TotalLicenses = l.TotalLicenses,
            UnitCost = l.TotalLicenses > 0 ? Math.Round(l.Cost / l.TotalLicenses, 2) : 0,
            AnnualCost = l.BillingCycle.Equals("Monthly", StringComparison.OrdinalIgnoreCase) ? l.Cost * 12 : l.Cost,
            RenewalCost = l.RenewalCost,
            DepartmentAttribution = "Cross-Departmental"
        }).OrderByDescending(c => c.AnnualCost).ToList();
    }

    public async Task<List<RenewalReportItemDto>> GetRenewalReportAsync(ReportFilterDto filter)
    {
        var query = _context.Renewals
            .Include(r => r.License)
            .Include(r => r.RenewedByUser)
            .AsNoTracking()
            .AsQueryable();

        if (filter.LicenseId.HasValue && filter.LicenseId > 0)
            query = query.Where(r => r.LicenseId == filter.LicenseId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(r => r.RenewalDate >= filter.StartDate.Value.ToUniversalTime());

        if (filter.EndDate.HasValue)
            query = query.Where(r => r.RenewalDate <= filter.EndDate.Value.ToUniversalTime());

        var items = await query.ToListAsync();

        return items.Select(r => new RenewalReportItemDto
        {
            RenewalId = r.Id,
            SoftwareName = r.License.SoftwareName,
            Vendor = r.License.Vendor,
            RenewalDate = r.RenewalDate,
            RenewalAmount = r.RenewalAmount,
            PreviousExpiryDate = r.PreviousExpiryDate,
            NewExpiryDate = r.NewExpiryDate,
            RenewedBy = r.RenewedByUser != null ? r.RenewedByUser.Email : "Admin",
            Status = r.Status,
            Remarks = r.Remarks
        }).OrderByDescending(r => r.RenewalDate).ToList();
    }

    public async Task<List<DepartmentReportItemDto>> GetDepartmentReportAsync(ReportFilterDto filter)
    {
        var employees = await _context.Employees
            .Include(e => e.LicenseAssignments)
                .ThenInclude(a => a.License)
            .AsNoTracking()
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(filter.Department) && filter.Department != "All")
        {
            employees = employees.Where(e => e.Department.ToLower() == filter.Department.ToLower()).ToList();
        }

        var groups = employees.GroupBy(e => e.Department);

        return groups.Select(g =>
        {
            var activeAssignments = g.SelectMany(e => e.LicenseAssignments.Where(a => a.Status == "Active")).ToList();
            var uniqueSoftware = activeAssignments.Select(a => a.License.SoftwareName).Distinct().ToList();
            var totalCost = activeAssignments.Sum(a => a.License.TotalLicenses > 0 ? a.License.Cost / a.License.TotalLicenses : 0);

            return new DepartmentReportItemDto
            {
                Department = g.Key,
                TotalEmployees = g.Count(),
                TotalLicensesAssigned = activeAssignments.Count,
                UniqueSoftwareCount = uniqueSoftware.Count,
                TotalCostAttributed = Math.Round(totalCost, 2),
                TopSoftwareAssigned = uniqueSoftware.Take(3).ToList()
            };
        }).OrderByDescending(d => d.TotalLicensesAssigned).ToList();
    }

    public async Task<byte[]> ExportReportCsvAsync(string reportType, ReportFilterDto filter)
    {
        var sb = new StringBuilder();

        switch (reportType.ToLower())
        {
            case "utilization":
                var utilData = await GetLicenseUtilizationReportAsync(filter);
                sb.AppendLine("License Code,Software Name,Vendor,Type,Total Seats,Assigned Seats,Available Seats,Utilization Rate (%),Unit Cost ($),Total Cost ($),Status");
                foreach (var item in utilData)
                {
                    sb.AppendLine($"\"{Escape(item.LicenseCode)}\",\"{Escape(item.SoftwareName)}\",\"{Escape(item.Vendor)}\",\"{Escape(item.LicenseType)}\",{item.TotalLicenses},{item.AssignedCount},{item.AvailableCount},{item.UtilizationRate},{item.UnitCost},{item.TotalCost},\"{Escape(item.Status)}\"");
                }
                break;

            case "expiry":
                var expData = await GetExpiryReportAsync(filter);
                sb.AppendLine("License Code,Software Name,Vendor,Expiry Date,Days Remaining,Assigned Seats,Renewal Cost ($),Urgency,Status");
                foreach (var item in expData)
                {
                    sb.AppendLine($"\"{Escape(item.LicenseCode)}\",\"{Escape(item.SoftwareName)}\",\"{Escape(item.Vendor)}\",\"{item.ExpiryDate:yyyy-MM-dd}\",{item.DaysRemaining},{item.AssignedCount},{item.RenewalCost},\"{Escape(item.UrgencyLevel)}\",\"{Escape(item.Status)}\"");
                }
                break;

            case "employee":
                var empData = await GetEmployeeLicenseReportAsync(filter);
                sb.AppendLine("Employee Code,Employee Name,Email,Department,Designation,Software Name,License Code,Assign Date,Expiry Date,Status");
                foreach (var item in empData)
                {
                    sb.AppendLine($"\"{Escape(item.EmployeeCode)}\",\"{Escape(item.EmployeeName)}\",\"{Escape(item.Email)}\",\"{Escape(item.Department)}\",\"{Escape(item.Designation)}\",\"{Escape(item.SoftwareName)}\",\"{Escape(item.LicenseCode)}\",\"{item.AssignDate:yyyy-MM-dd}\",\"{item.ExpiryDate:yyyy-MM-dd}\",\"{Escape(item.Status)}\"");
                }
                break;

            case "cost":
                var costData = await GetCostReportAsync(filter);
                sb.AppendLine("Category,Software Name,Vendor,Billing Cycle,Total Seats,Unit Cost ($),Annual Cost ($),Renewal Cost ($),Department Attribution");
                foreach (var item in costData)
                {
                    sb.AppendLine($"\"{Escape(item.Category)}\",\"{Escape(item.SoftwareName)}\",\"{Escape(item.Vendor)}\",\"{Escape(item.BillingCycle)}\",{item.TotalLicenses},{item.UnitCost},{item.AnnualCost},{item.RenewalCost},\"{Escape(item.DepartmentAttribution)}\"");
                }
                break;

            case "renewal":
                var renData = await GetRenewalReportAsync(filter);
                sb.AppendLine("Renewal ID,Software Name,Vendor,Renewal Date,Renewal Amount ($),Previous Expiry,New Expiry,Renewed By,Status,Remarks");
                foreach (var item in renData)
                {
                    sb.AppendLine($"{item.RenewalId},\"{Escape(item.SoftwareName)}\",\"{Escape(item.Vendor)}\",\"{item.RenewalDate:yyyy-MM-dd}\",{item.RenewalAmount},\"{item.PreviousExpiryDate:yyyy-MM-dd}\",\"{item.NewExpiryDate:yyyy-MM-dd}\",\"{Escape(item.RenewedBy)}\",\"{Escape(item.Status)}\",\"{Escape(item.Remarks)}\"");
                }
                break;

            case "department":
                var deptData = await GetDepartmentReportAsync(filter);
                sb.AppendLine("Department,Total Employees,Assigned Licenses,Unique Software Count,Total Attributed Cost ($),Top Software");
                foreach (var item in deptData)
                {
                    var topSw = string.Join(" | ", item.TopSoftwareAssigned);
                    sb.AppendLine($"\"{Escape(item.Department)}\",{item.TotalEmployees},{item.TotalLicensesAssigned},{item.UniqueSoftwareCount},{item.TotalCostAttributed},\"{Escape(topSw)}\"");
                }
                break;

            default:
                throw new InvalidOperationException($"Unsupported report type: {reportType}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Escape(string? value)
    {
        return value?.Replace("\"", "\"\"") ?? string.Empty;
    }
}
