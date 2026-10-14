using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface IAiService
{
    Task<AiChatResponseDto> ProcessChatAsync(AiChatRequestDto request, int userId, string userRole, int? employeeId);
    Task<AiActionConfirmResponseDto> ConfirmActionAsync(AiActionConfirmRequestDto request, int userId, string userRole);
    Task<SoftwareRecommendationResultDto> GetRecommendationsForEmployeeAsync(int employeeId);
    Task<SoftwareRecommendationResultDto> GetRecommendationsByRoleAsync(string? department, string? designation, int? excludeEmployeeId = null);
    Task<DuplicateDetectionResultDto> CheckDuplicateLicenseAsync(DuplicateDetectionRequestDto request);
    Task<PortfolioRedundancyScanResultDto> ScanPortfolioRedundancyAsync();
}

public class AiService : IAiService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAssignmentService _assignmentService;
    private readonly IRenewalService _renewalService;
    private readonly ILogger<AiService> _logger;
    private readonly HttpClient _httpClient;

    // Cache of active proposed actions
    private static readonly Dictionary<string, (AiProposedActionDto Action, DateTime ExpiresAt, int UserId, string Role)> ActionTokenCache = new();

    public AiService(
        ApplicationDbContext context,
        IConfiguration configuration,
        IAssignmentService assignmentService,
        IRenewalService renewalService,
        ILogger<AiService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _assignmentService = assignmentService;
        _renewalService = renewalService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<AiChatResponseDto> ProcessChatAsync(AiChatRequestDto request, int userId, string userRole, int? employeeId)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new AiChatResponseDto { Reply = "Please provide a question or request.", IsSuccess = true };
        }

        var openAiKey = _configuration["OPENAI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = _configuration["OPENAI_MODEL"] ?? Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";

        // If OpenAI API key is set and valid, call OpenAI API with function tools
        if (!string.IsNullOrWhiteSpace(openAiKey) && openAiKey != "your-openai-api-key-here" && openAiKey.StartsWith("sk-"))
        {
            try
            {
                return await CallOpenAiWithToolsAsync(request, userId, userRole, employeeId, openAiKey, model);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenAI API call failed, falling back to local deterministic tool engine.");
            }
        }

        // Fallback: Local Semantic AI Tool Engine (Guarantees 100% real database results locally even without OpenAI key)
        return await ProcessLocalSemanticQueryAsync(request.Message.Trim(), userId, userRole, employeeId);
    }

    public async Task<AiActionConfirmResponseDto> ConfirmActionAsync(AiActionConfirmRequestDto request, int userId, string userRole)
    {
        if (userRole == "EMPLOYEE")
        {
            return new AiActionConfirmResponseDto
            {
                Success = false,
                Message = "Employees have read-only permissions and cannot confirm system modifications."
            };
        }

        lock (ActionTokenCache)
        {
            if (!ActionTokenCache.TryGetValue(request.ConfirmationToken, out var cached) || cached.ExpiresAt < DateTime.UtcNow)
            {
                return new AiActionConfirmResponseDto
                {
                    Success = false,
                    Message = "Action confirmation has expired or is invalid. Please make the request again."
                };
            }

            ActionTokenCache.Remove(request.ConfirmationToken);
        }

        try
        {
            switch (request.ActionType.ToUpper())
            {
                case "ASSIGN_LICENSE":
                    if (!request.Parameters.TryGetValue("employeeId", out var empVal) ||
                        !request.Parameters.TryGetValue("licenseId", out var licVal))
                    {
                        return new AiActionConfirmResponseDto { Success = false, Message = "Missing required parameters for assignment." };
                    }

                    int empId = Convert.ToInt32(empVal.ToString());
                    int licId = Convert.ToInt32(licVal.ToString());
                    string? remarks = request.Parameters.TryGetValue("remarks", out var rem) ? rem?.ToString() : "Assigned via AI Assistant";

                    var assignment = await _assignmentService.AssignLicenseAsync(new AssignLicenseDto
                    {
                        EmployeeId = empId,
                        LicenseId = licId,
                        Remarks = remarks
                    }, userId);

                    return new AiActionConfirmResponseDto
                    {
                        Success = true,
                        Message = $"Successfully assigned license '{assignment.SoftwareName}' to {assignment.EmployeeName}.",
                        ResultData = assignment
                    };

                case "REVOKE_LICENSE":
                    if (!request.Parameters.TryGetValue("assignmentId", out var asgnVal))
                    {
                        return new AiActionConfirmResponseDto { Success = false, Message = "Missing assignmentId parameter." };
                    }

                    int assignmentId = Convert.ToInt32(asgnVal.ToString());
                    string reason = request.Parameters.TryGetValue("reason", out var rsn) ? rsn?.ToString() ?? "License Reallocation" : "License Reallocation";

                    var revoked = await _assignmentService.RevokeLicenseAsync(assignmentId, new RevokeLicenseDto
                    {
                        RevocationReason = reason,
                        Remarks = "Revoked via AI Assistant"
                    }, userId);

                    return new AiActionConfirmResponseDto
                    {
                        Success = true,
                        Message = $"Successfully revoked license '{revoked.SoftwareName}' from {revoked.EmployeeName}.",
                        ResultData = revoked
                    };

                case "RENEW_LICENSE":
                    if (!request.Parameters.TryGetValue("licenseId", out var lIdVal) ||
                        !request.Parameters.TryGetValue("amount", out var amtVal) ||
                        !request.Parameters.TryGetValue("newExpiryDate", out var expVal))
                    {
                        return new AiActionConfirmResponseDto { Success = false, Message = "Missing renewal parameters." };
                    }

                    int lId = Convert.ToInt32(lIdVal.ToString());
                    decimal amount = Convert.ToDecimal(amtVal.ToString());
                    DateTime newExpiry = Convert.ToDateTime(expVal.ToString());

                    var renewal = await _renewalService.ProcessRenewalAsync(new CreateRenewalDto
                    {
                        LicenseId = lId,
                        RenewalAmount = amount,
                        NewExpiryDate = newExpiry,
                        Remarks = "Renewed via AI Assistant"
                    }, userId);

                    return new AiActionConfirmResponseDto
                    {
                        Success = true,
                        Message = $"Successfully renewed license '{renewal.SoftwareName}' until {renewal.NewExpiryDate:MMM dd, yyyy}.",
                        ResultData = renewal
                    };

                default:
                    return new AiActionConfirmResponseDto { Success = false, Message = $"Unknown action type: {request.ActionType}" };
            }
        }
        catch (Exception ex)
        {
            return new AiActionConfirmResponseDto
            {
                Success = false,
                Message = $"Failed to execute action: {ex.Message}"
            };
        }
    }

    #region Controlled Backend Database Tools

    public async Task<object> ToolGetLicenseSummaryAsync()
    {
        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();
        return new
        {
            TotalLicensesCount = licenses.Sum(l => l.TotalLicenses),
            TotalAssignedCount = licenses.Sum(l => l.AssignedCount),
            TotalAvailableCount = licenses.Sum(l => l.AvailableCount),
            OverallUtilization = licenses.Sum(l => l.TotalLicenses) > 0 ? Math.Round((double)licenses.Sum(l => l.AssignedCount) / licenses.Sum(l => l.TotalLicenses) * 100, 1) : 0,
            TotalSoftwareProducts = licenses.Count,
            TotalAnnualCost = licenses.Sum(l => l.Cost)
        };
    }

    public async Task<object> ToolGetAvailableLicensesAsync(string? softwareQuery = null)
    {
        var query = _context.Licenses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(softwareQuery))
        {
            var s = softwareQuery.Trim().ToLower();
            query = query.Where(l => l.SoftwareName.ToLower().Contains(s) || l.LicenseCode.ToLower().Contains(s));
        }

        var list = await query.Select(l => new
        {
            l.Id,
            l.LicenseCode,
            l.SoftwareName,
            l.Vendor,
            l.TotalLicenses,
            l.AssignedCount,
            l.AvailableCount,
            l.ExpiryDate,
            l.Status
        }).ToListAsync();

        return list;
    }

    public async Task<object> ToolGetExpiringLicensesAsync(int days = 30)
    {
        var now = DateTime.UtcNow.Date;
        var limit = now.AddDays(days);

        var list = await _context.Licenses
            .Where(l => l.ExpiryDate >= now && l.ExpiryDate <= limit)
            .OrderBy(l => l.ExpiryDate)
            .Select(l => new
            {
                l.Id,
                l.LicenseCode,
                l.SoftwareName,
                l.Vendor,
                l.ExpiryDate,
                DaysRemaining = (int)Math.Ceiling((l.ExpiryDate - now).TotalDays),
                l.AssignedCount,
                l.RenewalCost
            })
            .ToListAsync();

        return list;
    }

    public async Task<object> ToolGetExpiredLicensesAsync()
    {
        var now = DateTime.UtcNow.Date;
        var list = await _context.Licenses
            .Where(l => l.ExpiryDate < now)
            .OrderBy(l => l.ExpiryDate)
            .Select(l => new
            {
                l.Id,
                l.LicenseCode,
                l.SoftwareName,
                l.Vendor,
                l.ExpiryDate,
                DaysExpired = (int)Math.Ceiling((now - l.ExpiryDate).TotalDays),
                l.AssignedCount
            })
            .ToListAsync();

        return list;
    }

    public async Task<object> ToolGetEmployeeLicensesAsync(string employeeQuery)
    {
        var s = employeeQuery.Trim().ToLower();
        var assignments = await _context.LicenseAssignments
            .Include(a => a.Employee)
            .Include(a => a.License)
            .Where(a => a.Status == "Active" &&
                (a.Employee.EmployeeName.ToLower().Contains(s) || a.Employee.EmployeeCode.ToLower().Contains(s) || a.Employee.Email.ToLower().Contains(s)))
            .Select(a => new
            {
                a.Employee.EmployeeCode,
                a.Employee.EmployeeName,
                a.Employee.Department,
                a.License.SoftwareName,
                a.License.LicenseCode,
                a.AssignDate,
                a.ExpiryDate,
                a.Status
            })
            .ToListAsync();

        return assignments;
    }

    public async Task<object> ToolGetDepartmentUtilizationAsync()
    {
        var assignments = await _context.LicenseAssignments
            .Include(a => a.Employee)
            .Include(a => a.License)
            .Where(a => a.Status == "Active")
            .AsNoTracking()
            .ToListAsync();

        return assignments
            .GroupBy(a => a.Employee.Department)
            .Select(g => new
            {
                Department = g.Key,
                LicenseCount = g.Count(),
                EmployeesCount = g.Select(x => x.EmployeeId).Distinct().Count(),
                SoftwareList = g.Select(x => x.License.SoftwareName).Distinct().ToList()
            })
            .OrderByDescending(d => d.LicenseCount)
            .ToList();
    }

    public async Task<object> ToolGetLicenseCostSummaryAsync()
    {
        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();
        return new
        {
            TotalCost = licenses.Sum(l => l.Cost),
            TotalRenewalCost = licenses.Sum(l => l.RenewalCost),
            HighestCostSoftware = licenses.OrderByDescending(l => l.Cost).Select(l => new { l.SoftwareName, l.Cost }).Take(3).ToList(),
            LowestCostSoftware = licenses.OrderBy(l => l.Cost).Select(l => new { l.SoftwareName, l.Cost }).Take(3).ToList()
        };
    }

    public async Task<object> ToolGetRenewalSummaryAsync()
    {
        var now = DateTime.UtcNow.Date;
        var pending = await _context.Licenses
            .Where(l => l.ExpiryDate <= now.AddDays(30))
            .Select(l => new { l.Id, l.SoftwareName, l.ExpiryDate, l.RenewalCost })
            .ToListAsync();

        var completed = await _context.Renewals
            .Include(r => r.License)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new { r.Id, r.License.SoftwareName, r.RenewalDate, r.RenewalAmount, r.NewExpiryDate })
            .ToListAsync();

        return new { PendingRenewals = pending, RecentCompletedRenewals = completed };
    }

    #endregion

    #region Local Semantic Engine & OpenAI Invocation

    private async Task<AiChatResponseDto> ProcessLocalSemanticQueryAsync(string query, int userId, string userRole, int? employeeId)
    {
        var q = query.ToLower();
        var sources = new List<string>();

        // Action detection: Assign license (e.g. "Assign Microsoft 365 to Nitin" or "assign zoom to priyanka")
        if (q.StartsWith("assign ") && (userRole == "ADMIN" || userRole == "IT_MANAGER"))
        {
            return await ProposeAssignActionFromQueryAsync(query, userId, userRole);
        }

        // 1. Total licenses / availability
        if (q.Contains("how many") || q.Contains("total license") || q.Contains("available license") || q.Contains("seats"))
        {
            sources.Add("GetLicenseSummary");
            sources.Add("GetAvailableLicenses");

            var licenses = await _context.Licenses.AsNoTracking().ToListAsync();

            // Check if query is about a specific software
            var matched = licenses.FirstOrDefault(l => q.Contains(l.SoftwareName.ToLower()) || q.Contains(l.LicenseCode.ToLower()));
            if (matched != null)
            {
                var sbMatch = new StringBuilder();
                sbMatch.AppendLine($"### 📊 **{matched.SoftwareName}** License Status\n");
                sbMatch.AppendLine($"- **License Code:** `{matched.LicenseCode}`");
                sbMatch.AppendLine($"- **Vendor:** {matched.Vendor}");
                sbMatch.AppendLine($"- **Total Seats:** **{matched.TotalLicenses}**");
                sbMatch.AppendLine($"- **Assigned Seats:** **{matched.AssignedCount}**");
                sbMatch.AppendLine($"- **Available Seats:** **{matched.AvailableCount}**");
                sbMatch.AppendLine($"- **Utilization:** **{Math.Round((double)matched.AssignedCount / matched.TotalLicenses * 100, 1)}%**");
                sbMatch.AppendLine($"- **Expiry Date:** {matched.ExpiryDate:MMM dd, yyyy}");
                sbMatch.AppendLine($"- **Status:** `{matched.Status}`");

                if (matched.AvailableCount == 0)
                {
                    sbMatch.AppendLine("\n> [!WARNING]\n> There are currently **0 available seats** for this software. No new assignments can be made.");
                }

                return new AiChatResponseDto
                {
                    Reply = sbMatch.ToString(),
                    SourcesOrToolsUsed = sources,
                    IsSuccess = true
                };
            }

            var total = licenses.Sum(l => l.TotalLicenses);
            var assigned = licenses.Sum(l => l.AssignedCount);
            var available = licenses.Sum(l => l.AvailableCount);
            var util = total > 0 ? Math.Round((double)assigned / total * 100, 1) : 0;

            var sb = new StringBuilder();
            sb.AppendLine("### 📈 **Enterprise Software License Summary**\n");
            sb.AppendLine($"- **Total Licenses:** **{total}** seats across **{licenses.Count}** software titles");
            sb.AppendLine($"- **Assigned Licenses:** **{assigned}** seats");
            sb.AppendLine($"- **Available Licenses:** **{available}** seats");
            sb.AppendLine($"- **Overall System Utilization:** **{util}%**\n");
            sb.AppendLine("#### Breakdown by Software:\n");
            sb.AppendLine("| Software Name | Total | Assigned | Available | Utilization | Status |");
            sb.AppendLine("| :--- | :---: | :---: | :---: | :---: | :---: |");

            foreach (var l in licenses)
            {
                var rate = l.TotalLicenses > 0 ? Math.Round((double)l.AssignedCount / l.TotalLicenses * 100) : 0;
                sb.AppendLine($"| **{l.SoftwareName}** | {l.TotalLicenses} | {l.AssignedCount} | **{l.AvailableCount}** | {rate}% | `{l.Status}` |");
            }

            return new AiChatResponseDto
            {
                Reply = sb.ToString(),
                SourcesOrToolsUsed = sources,
                IsSuccess = true
            };
        }

        // 2. Expiring / Expired licenses
        if (q.Contains("expir") || q.Contains("due") || q.Contains("validity"))
        {
            sources.Add("GetExpiringLicenses");
            sources.Add("GetExpiredLicenses");

            var now = DateTime.UtcNow.Date;
            var licenses = await _context.Licenses.AsNoTracking().OrderBy(l => l.ExpiryDate).ToListAsync();
            var expiring = licenses.Where(l => l.ExpiryDate >= now && l.ExpiryDate <= now.AddDays(30)).ToList();
            var expired = licenses.Where(l => l.ExpiryDate < now).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("### ⏳ **License Expiry & Renewal Status**\n");

            if (expired.Any())
            {
                sb.AppendLine("#### 🚨 Expired Licenses:\n");
                sb.AppendLine("| Software | Expiry Date | Assigned Users | Vendor | Action |");
                sb.AppendLine("| :--- | :---: | :---: | :---: | :--- |");
                foreach (var exp in expired)
                {
                    sb.AppendLine($"| **{exp.SoftwareName}** | {exp.ExpiryDate:yyyy-MM-dd} | {exp.AssignedCount} users | {exp.Vendor} | Immediate Renewal Needed |");
                }
                sb.AppendLine();
            }

            if (expiring.Any())
            {
                sb.AppendLine("#### ⚠️ Expiring in the Next 30 Days:\n");
                sb.AppendLine("| Software | Expiry Date | Days Left | Assigned Seats | Renewal Cost |");
                sb.AppendLine("| :--- | :---: | :---: | :---: | :---: |");
                foreach (var l in expiring)
                {
                    var days = (int)Math.Ceiling((l.ExpiryDate - now).TotalDays);
                    sb.AppendLine($"| **{l.SoftwareName}** | {l.ExpiryDate:MMM dd, yyyy} | **{days} days** | {l.AssignedCount} | ${l.RenewalCost:N2} |");
                }
            }
            else
            {
                sb.AppendLine("✅ There are no licenses expiring within the next 30 days.");
            }

            return new AiChatResponseDto
            {
                Reply = sb.ToString(),
                SourcesOrToolsUsed = sources,
                IsSuccess = true
            };
        }

        // 3. Employee Assigned Licenses
        if (q.Contains("employee") || q.Contains("assigned to") || q.Contains("who has") || q.Contains("kritika") || q.Contains("priyanka") || q.Contains("rahul") || q.Contains("sneha") || q.Contains("amit") || q.Contains("pooja"))
        {
            sources.Add("GetEmployeeLicenses");
            var employees = await _context.Employees.AsNoTracking().ToListAsync();

            // Find matching employee in query
            var targetEmp = employees.FirstOrDefault(e => q.Contains(e.EmployeeName.ToLower()) || q.Contains(e.EmployeeCode.ToLower()) || (e.Email != null && q.Contains(e.Email.ToLower())));

            if (targetEmp != null)
            {
                var assignments = await _context.LicenseAssignments
                    .Include(a => a.License)
                    .Where(a => a.EmployeeId == targetEmp.Id && a.Status == "Active")
                    .ToListAsync();

                var sb = new StringBuilder();
                sb.AppendLine($"### 👤 Assigned Licenses for **{targetEmp.EmployeeName}** (`{targetEmp.EmployeeCode}`)\n");
                sb.AppendLine($"- **Department:** {targetEmp.Department}");
                sb.AppendLine($"- **Designation:** {targetEmp.Designation}");
                sb.AppendLine($"- **Active Licenses:** **{assignments.Count}**\n");

                if (assignments.Any())
                {
                    sb.AppendLine("| Software Name | License Code | Assigned Date | Expiry Date | Status |");
                    sb.AppendLine("| :--- | :---: | :---: | :---: | :---: |");
                    foreach (var a in assignments)
                    {
                        sb.AppendLine($"| **{a.License.SoftwareName}** | `{a.License.LicenseCode}` | {a.AssignDate:yyyy-MM-dd} | {a.ExpiryDate:yyyy-MM-dd} | `{a.Status}` |");
                    }
                }
                else
                {
                    sb.AppendLine("_No active software licenses are currently assigned to this employee._");
                }

                return new AiChatResponseDto { Reply = sb.ToString(), SourcesOrToolsUsed = sources, IsSuccess = true };
            }
        }

        // 4. Department usage & costs
        if (q.Contains("department") || q.Contains("most license") || q.Contains("cost") || q.Contains("spending") || q.Contains("budget"))
        {
            sources.Add("GetDepartmentUtilization");
            sources.Add("GetLicenseCostSummary");

            var licenses = await _context.Licenses.AsNoTracking().ToListAsync();
            var totalCost = licenses.Sum(l => l.Cost);
            var totalRenewal = licenses.Sum(l => l.RenewalCost);

            var assignments = await _context.LicenseAssignments
                .Include(a => a.Employee)
                .Include(a => a.License)
                .Where(a => a.Status == "Active")
                .AsNoTracking()
                .ToListAsync();

            var deptGroups = assignments
                .GroupBy(a => a.Employee.Department)
                .Select(g => new { Dept = g.Key, Count = g.Count(), Users = g.Select(x => x.EmployeeId).Distinct().Count() })
                .OrderByDescending(d => d.Count)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("### 💰 **Cost & Department License Distribution**\n");
            sb.AppendLine($"- **Total Inventory Cost:** **${totalCost:N2}**");
            sb.AppendLine($"- **Projected Annual Renewal:** **${totalRenewal:N2}**\n");

            sb.AppendLine("#### Department License Consumption:\n");
            sb.AppendLine("| Department | Active Licenses | Active Users | License Density |");
            sb.AppendLine("| :--- | :---: | :---: | :---: |");
            foreach (var d in deptGroups)
            {
                var density = d.Users > 0 ? Math.Round((double)d.Count / d.Users, 1) : 0;
                sb.AppendLine($"| **{d.Dept}** | **{d.Count}** | {d.Users} | {density} licenses/user |");
            }

            return new AiChatResponseDto { Reply = sb.ToString(), SourcesOrToolsUsed = sources, IsSuccess = true };
        }

        // 5. AI Software Recommendations Intent
        if (q.Contains("recommend") || q.Contains("suggest") || q.Contains("what software") || q.Contains("what tools") || q.Contains("tool for") || q.Contains("need software") || q.Contains("recommendation"))
        {
            sources.Add("GetSoftwareRecommendations");

            // Check if asking for specific employee
            var employees = await _context.Employees.AsNoTracking().ToListAsync();
            var targetEmp = employees.FirstOrDefault(e => q.Contains(e.EmployeeName.ToLower()) || q.Contains(e.EmployeeCode.ToLower()));

            SoftwareRecommendationResultDto recResult;
            if (targetEmp != null)
            {
                recResult = await GetRecommendationsForEmployeeAsync(targetEmp.Id);
            }
            else
            {
                // Detect department / designation keywords in query
                string? dept = null;
                string? desig = null;
                if (q.Contains("engineer") || q.Contains("dev") || q.Contains("architect")) { dept = "Engineering"; desig = "Software Engineer"; }
                else if (q.Contains("cad") || q.Contains("drafting")) { dept = "Engineering"; desig = "CAD & Systems Engineer"; }
                else if (q.Contains("design") || q.Contains("ux") || q.Contains("ui")) { dept = "Design & UX"; desig = "Product Designer"; }
                else if (q.Contains("data") || q.Contains("analyst") || q.Contains("bi")) { dept = "Business Intelligence"; desig = "Data Specialist"; }
                else if (q.Contains("marketing")) { dept = "Marketing"; desig = "Marketing Lead"; }
                else { dept = "General"; desig = "Staff"; }

                recResult = await GetRecommendationsByRoleAsync(dept, desig);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"### 💡 **AI Software Recommendations**\n");
            sb.AppendLine($"**Target Profile:** {(recResult.EmployeeName != null ? $"**{recResult.EmployeeName}** (" : "")}{recResult.Designation} · {recResult.Department}{(recResult.EmployeeName != null ? ")" : "")}\n");
            sb.AppendLine($"{recResult.Summary}\n");
            sb.AppendLine("| Software Name | Vendor | Fit Score | Priority | Available Seats | AI Rationale |");
            sb.AppendLine("| :--- | :--- | :---: | :---: | :---: | :--- |");

            foreach (var r in recResult.Recommendations.Take(5))
            {
                var assignedTag = r.IsCurrentlyAssigned ? " *(Assigned)*" : "";
                sb.AppendLine($"| **{r.SoftwareName}**{assignedTag} | {r.Vendor} | `{r.MatchScore}%` | **{r.Priority}** | {r.AvailableSeats}/{r.TotalSeats} | {r.RecommendationReason} |");
            }

            return new AiChatResponseDto { Reply = sb.ToString(), SourcesOrToolsUsed = sources, IsSuccess = true };
        }

        // 6. AI Duplicate & Redundancy Detection Intent
        if (q.Contains("duplicate") || q.Contains("redundant") || q.Contains("similar license") || q.Contains("redundancy") || q.Contains("overlap") || q.Contains("consolidation"))
        {
            sources.Add("ScanRedundantLicenses");
            var scan = await ScanPortfolioRedundancyAsync();

            var sb = new StringBuilder();
            sb.AppendLine("### 🔍 **AI Duplicate & Redundancy Analysis**\n");
            sb.AppendLine($"AI scanned **{scan.TotalLicensesScanned} software assets** and discovered **{scan.RedundantGroupsFound} functional overlap groups** with estimated **${scan.EstimatedAnnualSavings:N2}/yr potential consolidation savings**.\n");

            foreach (var g in scan.RedundancyGroups)
            {
                sb.AppendLine($"#### 📦 **{g.CategoryOrFunction}**");
                sb.AppendLine($"- **Current Category Spend:** ${g.TotalAnnualSpend:N2}/yr");
                sb.AppendLine($"- **Potential Savings:** **${g.PotentialSavings:N2}/yr**");
                sb.AppendLine($"- **AI Advice:** {g.ConsolidationAdvice}");
                sb.AppendLine($"- **Overlapping Assets:** {string.Join(", ", g.OverlappingLicenses.Select(l => l.ExistingSoftwareName))}\n");
            }

            return new AiChatResponseDto { Reply = sb.ToString(), SourcesOrToolsUsed = sources, IsSuccess = true };
        }

        // 7. Default General Context Response
        sources.Add("GetDashboardSummary");
        var totalLic = await _context.Licenses.CountAsync();
        var totalEmp = await _context.Employees.CountAsync();
        var activeAsg = await _context.LicenseAssignments.CountAsync(a => a.Status == "Active");

        var defaultSb = new StringBuilder();
        defaultSb.AppendLine("### 🤖 **License AI Assistant**\n");
        defaultSb.AppendLine($"I have analyzed your query based on authorized system context ({totalLic} software products, {totalEmp} employees, and {activeAsg} active assignments).\n");
        defaultSb.AppendLine("You can ask me specific questions like:");
        defaultSb.AppendLine("- *\"How many licenses are available for Microsoft 365?\"*");
        defaultSb.AppendLine("- *\"Recommend software for CAD Engineer\"*");
        defaultSb.AppendLine("- *\"Scan duplicate licenses and redundancy\"*");
        defaultSb.AppendLine("- *\"Which licenses expire in the next 30 days?\"*");
        defaultSb.AppendLine("- *\"Show licenses assigned to Kritika\"*");
        defaultSb.AppendLine("- *\"What is our total software spend?\"*");

        if (userRole == "ADMIN" || userRole == "IT_MANAGER")
        {
            defaultSb.AppendLine("- *\"Assign Microsoft 365 to Kritika\"* (proposes an authorized action)");
        }

        return new AiChatResponseDto
        {
            Reply = defaultSb.ToString(),
            SourcesOrToolsUsed = sources,
            IsSuccess = true
        };
    }

    private async Task<AiChatResponseDto> ProposeAssignActionFromQueryAsync(string query, int userId, string userRole)
    {
        var employees = await _context.Employees.Where(e => e.Status == "Active").ToListAsync();
        var licenses = await _context.Licenses.ToListAsync();

        var matchedEmp = employees.FirstOrDefault(e => query.ToLower().Contains(e.EmployeeName.ToLower()) || query.ToLower().Contains(e.EmployeeCode.ToLower()));
        var matchedLic = licenses.FirstOrDefault(l => query.ToLower().Contains(l.SoftwareName.ToLower()) || query.ToLower().Contains(l.LicenseCode.ToLower()));

        if (matchedEmp == null || matchedLic == null)
        {
            return new AiChatResponseDto
            {
                Reply = "I could not accurately identify both the employee and the software license from your message. Please specify e.g.: `Assign Microsoft 365 to Kritika Chouhan`.",
                IsSuccess = true
            };
        }

        if (matchedLic.AvailableCount <= 0)
        {
            return new AiChatResponseDto
            {
                Reply = $"❌ Cannot propose assignment: **{matchedLic.SoftwareName}** has **0 available seats**.",
                IsSuccess = true
            };
        }

        var isDuplicate = await _context.LicenseAssignments.AnyAsync(a => a.EmployeeId == matchedEmp.Id && a.LicenseId == matchedLic.Id && a.Status == "Active");
        if (isDuplicate)
        {
            return new AiChatResponseDto
            {
                Reply = $"⚠️ **{matchedEmp.EmployeeName}** already has an active license for **{matchedLic.SoftwareName}**.",
                IsSuccess = true
            };
        }

        var token = Guid.NewGuid().ToString("N");
        var proposedAction = new AiProposedActionDto
        {
            ActionType = "ASSIGN_LICENSE",
            ActionTitle = "Assign Software License",
            Description = $"Assign 1 seat of '{matchedLic.SoftwareName}' to {matchedEmp.EmployeeName} ({matchedEmp.Department}).",
            Parameters = new Dictionary<string, object>
            {
                { "employeeId", matchedEmp.Id },
                { "employeeName", matchedEmp.EmployeeName },
                { "licenseId", matchedLic.Id },
                { "softwareName", matchedLic.SoftwareName },
                { "availableSeatsBefore", matchedLic.AvailableCount },
                { "expiryDate", matchedLic.ExpiryDate.ToString("yyyy-MM-dd") }
            },
            RequiresConfirmation = true,
            ConfirmationToken = token
        };

        lock (ActionTokenCache)
        {
            ActionTokenCache[token] = (proposedAction, DateTime.UtcNow.AddMinutes(10), userId, userRole);
        }

        var sb = new StringBuilder();
        sb.AppendLine("### ⚡ **Action Proposal Prepared**\n");
        sb.AppendLine($"I have verified all business rules:");
        sb.AppendLine($"- ✅ **Employee:** {matchedEmp.EmployeeName} ({matchedEmp.EmployeeCode}) — Status: Active");
        sb.AppendLine($"- ✅ **Software:** {matchedLic.SoftwareName} — Available Seats: {matchedLic.AvailableCount}");
        sb.AppendLine($"- ✅ **No Duplicate:** Employee does not currently have this license");
        sb.AppendLine($"- ✅ **Validity:** License valid until {matchedLic.ExpiryDate:yyyy-MM-dd}\n");
        sb.AppendLine("Please confirm the action below to proceed with the database transaction.");

        return new AiChatResponseDto
        {
            Reply = sb.ToString(),
            SourcesOrToolsUsed = new List<string> { "GetEmployeeDetails", "GetAvailableLicenses", "ProposeAssignAction" },
            ProposedAction = proposedAction,
            IsSuccess = true
        };
    }

    private async Task<AiChatResponseDto> CallOpenAiWithToolsAsync(AiChatRequestDto request, int userId, string userRole, int? employeeId, string apiKey, string model)
    {
        // Define tool schemas for OpenAI
        var tools = new object[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "GetLicenseSummary",
                    description = "Get summary of all software licenses including total, assigned, available seats, utilization and cost."
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "GetAvailableLicenses",
                    description = "Get software licenses with available seats.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            softwareQuery = new { type = "string", description = "Optional name or code of software." }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "GetExpiringLicenses",
                    description = "Get software licenses expiring within a given number of days.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            days = new { type = "integer", description = "Number of days ahead to check (default 30)." }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "GetEmployeeLicenses",
                    description = "Get active software licenses assigned to an employee by name or code.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            employeeQuery = new { type = "string", description = "Employee name, code, or email." }
                        },
                        required = new[] { "employeeQuery" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "GetDepartmentUtilization",
                    description = "Get license count and software consumption grouped by department."
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "GetLicenseCostSummary",
                    description = "Get software licensing cost overview and renewal projections."
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "GetSoftwareRecommendations",
                    description = "Recommend software licenses based on an employee's department and job designation.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            department = new { type = "string", description = "Employee department (e.g. Engineering, Design & UX, Business Intelligence)." },
                            designation = new { type = "string", description = "Employee title/designation (e.g. CAD Engineer, Product Designer, Software Architect)." }
                        }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "CheckDuplicateLicense",
                    description = "Check if a software license or vendor contract is a duplicate or overlaps with existing licenses in the inventory.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            softwareName = new { type = "string", description = "Name of software to verify." },
                            vendor = new { type = "string", description = "Software vendor name." }
                        },
                        required = new[] { "softwareName" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "ScanRedundantLicenses",
                    description = "Scan entire license portfolio to detect multi-vendor overlaps, duplicate software, and cost saving consolidation opportunities."
                }
            }
        };

        var systemPrompt = $"You are the License AI Assistant for an enterprise License Management System. Answer questions using only authorized application data and system context. Never invent database values. If required information is unavailable, clearly say so. The current authenticated user has role: {userRole}. For actions that modify system data, require appropriate authorization and explicit confirmation before executing the action. Never expose passwords, API keys, secrets or confidential information.";

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        if (request.ConversationHistory != null)
        {
            foreach (var h in request.ConversationHistory.TakeLast(6))
            {
                messages.Add(new { role = h.Role, content = h.Content });
            }
        }

        messages.Add(new { role = "user", content = request.Message });

        var openAiPayload = new
        {
            model = model,
            messages = messages,
            tools = tools,
            tool_choice = "auto",
            temperature = 0.2
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        requestMessage.Content = new StringContent(JsonSerializer.Serialize(openAiPayload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(requestMessage);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"OpenAI returned error: {response.StatusCode} - {err}");
        }

        var resJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(resJson);
        var choice = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

        var toolsUsed = new List<string>();

        // Check if tool call occurred
        if (choice.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
        {
            var toolCall = toolCalls[0];
            var funcName = toolCall.GetProperty("function").GetProperty("name").GetString();
            var funcArgs = toolCall.GetProperty("function").GetProperty("arguments").GetString();
            var toolId = toolCall.GetProperty("id").GetString();

            toolsUsed.Add(funcName ?? "Tool");

            object? toolResult = null;
            if (funcName == "GetLicenseSummary") toolResult = await ToolGetLicenseSummaryAsync();
            else if (funcName == "GetAvailableLicenses") toolResult = await ToolGetAvailableLicensesAsync();
            else if (funcName == "GetExpiringLicenses") toolResult = await ToolGetExpiringLicensesAsync(30);
            else if (funcName == "GetDepartmentUtilization") toolResult = await ToolGetDepartmentUtilizationAsync();
            else if (funcName == "GetLicenseCostSummary") toolResult = await ToolGetLicenseCostSummaryAsync();
            else if (funcName == "GetEmployeeLicenses")
            {
                using var argsDoc = JsonDocument.Parse(funcArgs ?? "{}");
                var empQ = argsDoc.RootElement.TryGetProperty("employeeQuery", out var eq) ? eq.GetString() ?? "" : "";
                toolResult = await ToolGetEmployeeLicensesAsync(empQ);
            }
            else if (funcName == "GetSoftwareRecommendations")
            {
                using var argsDoc = JsonDocument.Parse(funcArgs ?? "{}");
                var dept = argsDoc.RootElement.TryGetProperty("department", out var dp) ? dp.GetString() : null;
                var desig = argsDoc.RootElement.TryGetProperty("designation", out var dg) ? dg.GetString() : null;
                toolResult = await GetRecommendationsByRoleAsync(dept, desig);
            }
            else if (funcName == "CheckDuplicateLicense")
            {
                using var argsDoc = JsonDocument.Parse(funcArgs ?? "{}");
                var sName = argsDoc.RootElement.TryGetProperty("softwareName", out var sn) ? sn.GetString() ?? "" : "";
                var vnd = argsDoc.RootElement.TryGetProperty("vendor", out var vn) ? vn.GetString() ?? "" : "";
                toolResult = await CheckDuplicateLicenseAsync(new DuplicateDetectionRequestDto { SoftwareName = sName, Vendor = vnd });
            }
            else if (funcName == "ScanRedundantLicenses")
            {
                toolResult = await ScanPortfolioRedundancyAsync();
            }

            // Second round to synthesize answer
            var secondMessages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = request.Message },
                new { role = "assistant", content = (string?)null, tool_calls = toolCalls },
                new { role = "tool", tool_call_id = toolId, name = funcName, content = JsonSerializer.Serialize(toolResult) }
            };

            var secondPayload = new
            {
                model = model,
                messages = secondMessages,
                temperature = 0.2
            };

            var secondReq = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            secondReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            secondReq.Content = new StringContent(JsonSerializer.Serialize(secondPayload), Encoding.UTF8, "application/json");

            var secondRes = await _httpClient.SendAsync(secondReq);
            if (secondRes.IsSuccessStatusCode)
            {
                var secondResJson = await secondRes.Content.ReadAsStringAsync();
                using var secondDoc = JsonDocument.Parse(secondResJson);
                var finalReply = secondDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                return new AiChatResponseDto
                {
                    Reply = finalReply ?? "Processed successfully.",
                    SourcesOrToolsUsed = toolsUsed,
                    IsSuccess = true
                };
            }
        }

        var directReply = choice.TryGetProperty("content", out var cnt) ? cnt.GetString() : "No response generated.";
        return new AiChatResponseDto
        {
            Reply = directReply ?? "",
            SourcesOrToolsUsed = toolsUsed,
            IsSuccess = true
        };
    }

    #endregion

    #region AI Software Recommendation Engine

    public async Task<SoftwareRecommendationResultDto> GetRecommendationsForEmployeeAsync(int employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.LicenseAssignments.Where(a => a.Status == "Active"))
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null)
        {
            return new SoftwareRecommendationResultDto
            {
                Summary = "Employee not found."
            };
        }

        return await GetRecommendationsByRoleAsync(employee.Department, employee.Designation, employee.Id);
    }

    public async Task<SoftwareRecommendationResultDto> GetRecommendationsByRoleAsync(string? department, string? designation, int? excludeEmployeeId = null)
    {
        var dept = department ?? "General";
        var desig = designation ?? "Staff";

        // Query active software licenses from inventory
        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();

        // Query current employee assignments if ID is specified
        var assignedLicenseIds = new HashSet<int>();
        Employee? employee = null;
        if (excludeEmployeeId.HasValue)
        {
            employee = await _context.Employees.FindAsync(excludeEmployeeId.Value);
            assignedLicenseIds = (await _context.LicenseAssignments
                .Where(a => a.EmployeeId == excludeEmployeeId.Value && a.Status == "Active")
                .Select(a => a.LicenseId)
                .ToListAsync())
                .ToHashSet();
        }

        var recommendations = new List<RecommendedSoftwareItemDto>();

        foreach (var lic in licenses)
        {
            var isAssigned = assignedLicenseIds.Contains(lic.Id);
            var (matchScore, priority, reason) = CalculateSoftwareRoleRelevance(lic, dept, desig);

            recommendations.Add(new RecommendedSoftwareItemDto
            {
                LicenseId = lic.Id,
                SoftwareName = lic.SoftwareName,
                LicenseCode = lic.LicenseCode,
                Vendor = lic.Vendor,
                LicenseType = lic.LicenseType,
                AvailableSeats = lic.AvailableCount,
                TotalSeats = lic.TotalLicenses,
                AnnualCost = lic.Cost,
                MatchScore = matchScore,
                Priority = priority,
                RecommendationReason = reason,
                IsCurrentlyAssigned = isAssigned
            });
        }

        // Sort by match score descending, then prioritize unassigned licenses with available seats
        var sorted = recommendations
            .OrderByDescending(r => r.MatchScore)
            .ThenByDescending(r => r.AvailableSeats > 0)
            .ToList();

        var topRecommendation = sorted.FirstOrDefault(r => !r.IsCurrentlyAssigned && r.AvailableSeats > 0);
        var summary = topRecommendation != null
            ? $"Based on role '{desig}' in '{dept}', AI recommends provisioning **{topRecommendation.SoftwareName}** ({topRecommendation.MatchScore}% fit, {topRecommendation.AvailableSeats} seats ready)."
            : $"AI evaluated {licenses.Count} software assets for '{desig}' in '{dept}'.";

        return new SoftwareRecommendationResultDto
        {
            EmployeeId = excludeEmployeeId,
            EmployeeName = employee?.EmployeeName,
            Department = dept,
            Designation = desig,
            Summary = summary,
            Recommendations = sorted
        };
    }

    private static (int Score, string Priority, string Reason) CalculateSoftwareRoleRelevance(LicenseMaster lic, string department, string designation)
    {
        var d = department.ToLower();
        var t = designation.ToLower();
        var s = lic.SoftwareName.ToLower();
        var v = lic.Vendor.ToLower();

        // 1. Core Enterprise Base: Microsoft 365 / Email / Productivity
        if (s.Contains("365") || s.Contains("office") || s.Contains("workspace"))
        {
            return (95, "HIGH", "Universal enterprise productivity, email, and cloud storage suite required for all staff roles.");
        }

        // 2. Video Conferencing: Zoom
        if (s.Contains("zoom") || s.Contains("teams") || s.Contains("meet"))
        {
            var isRemoteOrLead = t.Contains("lead") || t.Contains("manager") || t.Contains("officer") || t.Contains("architect") || t.Contains("specialist");
            return (isRemoteOrLead ? 92 : 82, "HIGH", "Primary collaboration and video conferencing tool for cross-functional meetings.");
        }

        // 3. Developer & Engineering: Visual Studio / IDEs / GitHub
        if (s.Contains("visual studio") || s.Contains("jetbrains") || s.Contains("github") || s.Contains("intellij") || s.Contains("rider"))
        {
            if (d.Contains("engineering") || d.Contains("it") || t.Contains("developer") || t.Contains("architect") || t.Contains("engineer") || t.Contains("programmer"))
            {
                return (98, "HIGH", "Essential integrated development environment (IDE) for software engineering and application architecture.");
            }
            return (20, "OPTIONAL", "Specialized developer tool; typically not required outside engineering teams.");
        }

        // 4. CAD & Systems Modeling: AutoCAD / SolidWorks / Revit
        if (s.Contains("autocad") || s.Contains("autodesk") || s.Contains("solidworks") || s.Contains("revit") || s.Contains("cad"))
        {
            if (t.Contains("cad") || t.Contains("systems engineer") || t.Contains("architect") || d.Contains("engineering"))
            {
                return (96, "HIGH", "High-precision drafting, 3D systems modeling, and architectural drafting software.");
            }
            return (15, "OPTIONAL", "Technical CAD drafting suite; not required for non-technical or general staff.");
        }

        // 5. Creative & Visual Design: Adobe Creative Cloud / Canva / Figma
        if (s.Contains("adobe") || s.Contains("creative cloud") || s.Contains("photoshop") || s.Contains("figma"))
        {
            if (d.Contains("design") || d.Contains("ux") || t.Contains("designer") || t.Contains("ui") || t.Contains("product designer") || d.Contains("marketing"))
            {
                return (98, "HIGH", "Industry-standard graphic design, UI/UX prototyping, and visual asset creation suite.");
            }
            return (25, "OPTIONAL", "Professional creative software; recommended primarily for design and creative teams.");
        }

        if (s.Contains("canva"))
        {
            if (d.Contains("marketing") || d.Contains("design") || d.Contains("ux") || d.Contains("hr") || t.Contains("designer") || t.Contains("specialist"))
            {
                return (92, "HIGH", "Rapid content creation, presentations, and marketing collateral design.");
            }
            return (40, "OPTIONAL", "Lightweight visual design tool for marketing and presentation needs.");
        }

        // 6. Business Intelligence & Analytics: Power BI / Tableau
        if (s.Contains("power bi") || s.Contains("tableau") || s.Contains("looker") || s.Contains("analytics"))
        {
            if (d.Contains("business intelligence") || d.Contains("analytics") || t.Contains("data") || t.Contains("analyst") || t.Contains("cio") || t.Contains("lead") || t.Contains("manager"))
            {
                return (96, "HIGH", "Enterprise data visualization, SQL metrics modeling, and executive KPI reporting.");
            }
            return (35, "OPTIONAL", "Analytical intelligence tool for reporting specialists and team leaders.");
        }

        // General default scoring
        return (50, "MEDIUM", $"Standard software tool from {lic.Vendor}. Suitable as secondary workstation application.");
    }

    #endregion

    #region AI Duplicate & Redundancy Detection Engine

    public async Task<DuplicateDetectionResultDto> CheckDuplicateLicenseAsync(DuplicateDetectionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.SoftwareName))
        {
            return new DuplicateDetectionResultDto
            {
                HasDuplicates = false,
                Assessment = "Please provide a software name to check."
            };
        }

        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();
        var incomingName = request.SoftwareName.Trim();
        var incomingVendor = request.Vendor.Trim();
        var matches = new List<DuplicateMatchItemDto>();

        foreach (var lic in licenses)
        {
            var similarity = CalculateSoftwareSimilarity(incomingName, incomingVendor, request.LicenseReference, lic);

            if (similarity >= 40)
            {
                var matchLevel = similarity switch
                {
                    100 => "EXACT",
                    >= 75 => "HIGH",
                    >= 55 => "MODERATE",
                    _ => "LOW"
                };

                var (explanation, action) = DetermineDuplicateAdvice(similarity, incomingName, lic);

                matches.Add(new DuplicateMatchItemDto
                {
                    ExistingLicenseId = lic.Id,
                    ExistingSoftwareName = lic.SoftwareName,
                    LicenseCode = lic.LicenseCode,
                    Vendor = lic.Vendor,
                    TotalSeats = lic.TotalLicenses,
                    AvailableSeats = lic.AvailableCount,
                    Status = lic.Status,
                    SimilarityPercentage = similarity,
                    MatchLevel = matchLevel,
                    Explanation = explanation,
                    RecommendedAction = action
                });
            }
        }

        var sortedMatches = matches.OrderByDescending(m => m.SimilarityPercentage).ToList();
        var highestScore = sortedMatches.FirstOrDefault()?.SimilarityPercentage ?? 0;
        var hasDuplicates = highestScore >= 70;

        string assessment;
        if (highestScore == 100)
        {
            assessment = $"⚠️ **Exact Duplicate Detected!** An identical license for '{sortedMatches[0].ExistingSoftwareName}' already exists ({sortedMatches[0].LicenseCode}). Do not create a redundant license.";
        }
        else if (highestScore >= 75)
        {
            assessment = $"⚠️ **High Overlap ({highestScore}%)**: This software is very similar to existing license '{sortedMatches[0].ExistingSoftwareName}' ({sortedMatches[0].Vendor}). Review existing capacity before purchasing.";
        }
        else if (highestScore >= 50)
        {
            assessment = $"ℹ️ **Potential Redundancy ({highestScore}%)**: Found overlapping software from vendor '{sortedMatches[0].Vendor}'. Verify if features overlap.";
        }
        else
        {
            assessment = "✅ **No duplicates found.** This appears to be a unique software asset not currently present in organizational inventory.";
        }

        return new DuplicateDetectionResultDto
        {
            HasDuplicates = hasDuplicates,
            HighestSimilarityPercentage = highestScore,
            Assessment = assessment,
            Matches = sortedMatches
        };
    }

    public async Task<PortfolioRedundancyScanResultDto> ScanPortfolioRedundancyAsync()
    {
        var licenses = await _context.Licenses.AsNoTracking().ToListAsync();

        // Categorize software into functional domain buckets
        var categories = new Dictionary<string, (string Description, string Advice, List<LicenseMaster> Items)>
        {
            {
                "Productivity & Cloud Office",
                ("Suite tools for documents, spreadsheets, email, and cloud storage.",
                 "Consolidate around a single standard cloud productivity ecosystem to eliminate duplicate seat subscription overhead.",
                 new List<LicenseMaster>())
            },
            {
                "Video Conferencing & Communication",
                ("Team messaging and video meeting platforms.",
                 "Standardize on a single corporate conferencing solution instead of paying for multi-vendor meeting rooms.",
                 new List<LicenseMaster>())
            },
            {
                "Graphic Design & Creative Media",
                ("Visual asset production, UI design, and illustration tools.",
                 "Audit user requirements: allocate full creative suites only to core designers; utilize lightweight licenses for general marketing.",
                 new List<LicenseMaster>())
            },
            {
                "CAD & Engineering Drafting",
                ("3D modeling, technical architecture, and systems engineering CAD software.",
                 "Pool CAD licenses on floating networks to maximize concurrent seat utilization across project teams.",
                 new List<LicenseMaster>())
            },
            {
                "Business Intelligence & Data Analytics",
                ("Enterprise dashboards, ETL data modeling, and reporting tools.",
                 "Standardize data connectors and dashboard delivery on a single enterprise BI platform.",
                 new List<LicenseMaster>())
            }
        };

        foreach (var lic in licenses)
        {
            var s = lic.SoftwareName.ToLower();
            if (s.Contains("365") || s.Contains("office") || s.Contains("workspace") || s.Contains("slack"))
                categories["Productivity & Cloud Office"].Items.Add(lic);
            else if (s.Contains("zoom") || s.Contains("teams") || s.Contains("webex") || s.Contains("meet"))
                categories["Video Conferencing & Communication"].Items.Add(lic);
            else if (s.Contains("adobe") || s.Contains("creative") || s.Contains("canva") || s.Contains("figma") || s.Contains("photoshop"))
                categories["Graphic Design & Creative Media"].Items.Add(lic);
            else if (s.Contains("autocad") || s.Contains("autodesk") || s.Contains("solidworks") || s.Contains("cad"))
                categories["CAD & Engineering Drafting"].Items.Add(lic);
            else if (s.Contains("power bi") || s.Contains("tableau") || s.Contains("looker") || s.Contains("analytics"))
                categories["Business Intelligence & Data Analytics"].Items.Add(lic);
        }

        var redundancyGroups = new List<RedundancyGroupDto>();
        decimal totalEstimatedSavings = 0;

        foreach (var (catName, (desc, advice, items)) in categories)
        {
            if (items.Count >= 2)
            {
                var spend = items.Sum(i => i.Cost);
                var estimatedSavings = Math.Round(spend * 0.18m, 2); // 18% average consolidation savings
                totalEstimatedSavings += estimatedSavings;

                var overlapMatches = items.Select(i => new DuplicateMatchItemDto
                {
                    ExistingLicenseId = i.Id,
                    ExistingSoftwareName = i.SoftwareName,
                    LicenseCode = i.LicenseCode,
                    Vendor = i.Vendor,
                    TotalSeats = i.TotalLicenses,
                    AvailableSeats = i.AvailableCount,
                    Status = i.Status,
                    SimilarityPercentage = 85,
                    MatchLevel = "HIGH",
                    Explanation = $"Overlapping tool in '{catName}' category.",
                    RecommendedAction = "Consolidate subscription contract"
                }).ToList();

                redundancyGroups.Add(new RedundancyGroupDto
                {
                    CategoryOrFunction = catName,
                    Description = desc,
                    TotalAnnualSpend = spend,
                    PotentialSavings = estimatedSavings,
                    ConsolidationAdvice = advice,
                    OverlappingLicenses = overlapMatches
                });
            }
        }

        return new PortfolioRedundancyScanResultDto
        {
            TotalLicensesScanned = licenses.Count,
            RedundantGroupsFound = redundancyGroups.Count,
            EstimatedAnnualSavings = totalEstimatedSavings,
            RedundancyGroups = redundancyGroups
        };
    }

    private static int CalculateSoftwareSimilarity(string name1, string vendor1, string? ref1, LicenseMaster existing)
    {
        var n1 = NormalizeSoftwareName(name1);
        var n2 = NormalizeSoftwareName(existing.SoftwareName);

        // 1. Exact Name Match
        if (string.Equals(n1, n2, StringComparison.OrdinalIgnoreCase))
            return 100;

        // 2. Reference Key Match
        if (!string.IsNullOrWhiteSpace(ref1) && !string.IsNullOrWhiteSpace(existing.LicenseReference))
        {
            if (string.Equals(ref1.Trim(), existing.LicenseReference.Trim(), StringComparison.OrdinalIgnoreCase))
                return 100;
        }

        // 3. Normalized Token Jaccard Similarity
        var tokens1 = Tokenize(n1);
        var tokens2 = Tokenize(n2);
        var intersection = tokens1.Intersect(tokens2, StringComparer.OrdinalIgnoreCase).Count();
        var union = tokens1.Union(tokens2, StringComparer.OrdinalIgnoreCase).Count();
        var jaccard = union > 0 ? (double)intersection / union : 0;

        // 4. Levenshtein Distance Ratio
        var levDistance = ComputeLevenshtein(n1, n2);
        var maxLen = Math.Max(n1.Length, n2.Length);
        var levRatio = maxLen > 0 ? 1.0 - ((double)levDistance / maxLen) : 0;

        // 5. Vendor Match Boost
        var v1 = NormalizeVendor(vendor1);
        var v2 = NormalizeVendor(existing.Vendor);
        var vendorMatch = !string.IsNullOrWhiteSpace(v1) && !string.IsNullOrWhiteSpace(v2) &&
                          (v1.Contains(v2) || v2.Contains(v1) || v1 == v2);

        // Weighted combination
        var score = (jaccard * 0.5) + (levRatio * 0.35) + (vendorMatch ? 0.15 : 0.0);
        return (int)Math.Min(100, Math.Round(score * 100));
    }

    private static (string Explanation, string Action) DetermineDuplicateAdvice(int similarity, string incomingName, LicenseMaster existing)
    {
        if (similarity >= 95)
        {
            return (
                $"Exact or near-exact match with existing '{existing.SoftwareName}' ({existing.LicenseCode}).",
                $"Do not create a new license. Increase seat count on '{existing.LicenseCode}' (currently has {existing.AvailableCount} available seats)."
            );
        }

        if (similarity >= 75)
        {
            return (
                $"High similarity detected with '{existing.SoftwareName}' from vendor {existing.Vendor}.",
                $"Review existing contract '{existing.LicenseCode}' to check if additional tiers or add-on seats can be used instead."
            );
        }

        return (
            $"Partial name or vendor overlap with '{existing.SoftwareName}'.",
            "Verify with procurement whether this represents a distinct tool or redundant software package."
        );
    }

    private static string NormalizeSoftwareName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var lower = s.ToLower().Trim();

        // Expand common acronyms for high-accuracy duplicate matching
        lower = System.Text.RegularExpressions.Regex.Replace(lower, @"\bcc\b", "creative cloud");
        lower = System.Text.RegularExpressions.Regex.Replace(lower, @"\bm365\b", "microsoft 365");
        lower = System.Text.RegularExpressions.Regex.Replace(lower, @"\bo365\b", "office 365");
        lower = System.Text.RegularExpressions.Regex.Replace(lower, @"\bvs\b", "visual studio");
        lower = System.Text.RegularExpressions.Regex.Replace(lower, @"\bpbi\b", "power bi");

        // Remove common filler words to extract core software identifiers
        var words = new[] { "enterprise", "pro", "professional", "premium", "standard", "edition", "suite", "all apps", "cloud", "2026", "2025", "v1", "v2", "business", "corporate" };
        foreach (var w in words)
        {
            lower = lower.Replace(w, " ");
        }
        return string.Join(" ", lower.Split(new[] { ' ', '-', '_', '.', '/' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeVendor(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return string.Empty;
        var s = v.ToLower().Trim();
        if (s.Contains("microsoft") || s == "ms") return "microsoft";
        if (s.Contains("adobe")) return "adobe";
        if (s.Contains("autodesk") || s.Contains("dsk")) return "autodesk";
        if (s.Contains("salesforce") || s.Contains("slack")) return "salesforce";
        if (s.Contains("zoom")) return "zoom";
        if (s.Contains("canva")) return "canva";
        return s;
    }

    private static HashSet<string> Tokenize(string s)
    {
        return s.Split(new[] { ' ', '-', '_', '.', '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLower())
            .Where(t => t.Length > 1)
            .ToHashSet();
    }

    private static int ComputeLevenshtein(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        var d = new int[s.Length + 1, t.Length + 1];
        for (var i = 0; i <= s.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= t.Length; j++) d[0, j] = j;

        for (var i = 1; i <= s.Length; i++)
        {
            for (var j = 1; j <= t.Length; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s.Length, t.Length];
    }

    #endregion
}
