using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface ILicenseService
{
    Task<PagedResult<LicenseDto>> GetLicensesAsync(string? search, string? licenseType, string? vendor, string? status, int pageNumber = 1, int pageSize = 10, string? sortBy = "software", bool sortDesc = false);
    Task<LicenseDetailDto> GetLicenseByIdAsync(int id);
    Task<LicenseDto> CreateLicenseAsync(CreateLicenseDto request, int? currentUserId);
    Task<LicenseDto> UpdateLicenseAsync(int id, UpdateLicenseDto request, int? currentUserId);
    Task<bool> DeleteLicenseAsync(int id, int? currentUserId);
    Task<List<string>> GetVendorsAsync();
    Task<List<string>> GetLicenseTypesAsync();
}

public class LicenseService : ILicenseService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public LicenseService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PagedResult<LicenseDto>> GetLicensesAsync(string? search, string? licenseType, string? vendor, string? status, int pageNumber = 1, int pageSize = 10, string? sortBy = "software", bool sortDesc = false)
    {
        var query = _context.Licenses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(l =>
                l.SoftwareName.ToLower().Contains(s) ||
                l.LicenseCode.ToLower().Contains(s) ||
                l.Vendor.ToLower().Contains(s) ||
                (l.LicenseReference != null && l.LicenseReference.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(licenseType) && licenseType != "All")
        {
            query = query.Where(l => l.LicenseType.ToLower() == licenseType.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(vendor) && vendor != "All")
        {
            query = query.Where(l => l.Vendor.ToLower() == vendor.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(l => l.Status.ToLower() == status.Trim().ToLower());
        }

        query = sortBy?.ToLower() switch
        {
            "code" => sortDesc ? query.OrderByDescending(l => l.LicenseCode) : query.OrderBy(l => l.LicenseCode),
            "vendor" => sortDesc ? query.OrderByDescending(l => l.Vendor) : query.OrderBy(l => l.Vendor),
            "expiry" => sortDesc ? query.OrderByDescending(l => l.ExpiryDate) : query.OrderBy(l => l.ExpiryDate),
            "total" => sortDesc ? query.OrderByDescending(l => l.TotalLicenses) : query.OrderBy(l => l.TotalLicenses),
            "available" => sortDesc ? query.OrderByDescending(l => l.AvailableCount) : query.OrderBy(l => l.AvailableCount),
            "cost" => sortDesc ? query.OrderByDescending(l => l.Cost) : query.OrderBy(l => l.Cost),
            "status" => sortDesc ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
            _ => sortDesc ? query.OrderByDescending(l => l.SoftwareName) : query.OrderBy(l => l.SoftwareName),
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LicenseDto
            {
                Id = l.Id,
                LicenseCode = l.LicenseCode,
                SoftwareName = l.SoftwareName,
                LicenseType = l.LicenseType,
                Vendor = l.Vendor,
                PurchaseDate = l.PurchaseDate,
                ExpiryDate = l.ExpiryDate,
                TotalLicenses = l.TotalLicenses,
                AssignedCount = l.AssignedCount,
                AvailableCount = l.AvailableCount,
                Cost = l.Cost,
                BillingCycle = l.BillingCycle,
                RenewalCost = l.RenewalCost,
                LicenseReference = l.LicenseReference,
                Status = l.Status,
                Notes = l.Notes,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<LicenseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<LicenseDetailDto> GetLicenseByIdAsync(int id)
    {
        var license = await _context.Licenses
            .Include(l => l.LicenseAssignments)
                .ThenInclude(a => a.Employee)
            .Include(l => l.LicenseAssignments)
                .ThenInclude(a => a.AssignedByUser)
            .Include(l => l.Renewals)
                .ThenInclude(r => r.RenewedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (license == null)
        {
            throw new KeyNotFoundException($"License with ID {id} not found.");
        }

        return new LicenseDetailDto
        {
            Id = license.Id,
            LicenseCode = license.LicenseCode,
            SoftwareName = license.SoftwareName,
            LicenseType = license.LicenseType,
            Vendor = license.Vendor,
            PurchaseDate = license.PurchaseDate,
            ExpiryDate = license.ExpiryDate,
            TotalLicenses = license.TotalLicenses,
            AssignedCount = license.AssignedCount,
            AvailableCount = license.AvailableCount,
            Cost = license.Cost,
            BillingCycle = license.BillingCycle,
            RenewalCost = license.RenewalCost,
            LicenseReference = license.LicenseReference,
            Status = license.Status,
            Notes = license.Notes,
            CreatedAt = license.CreatedAt,
            UpdatedAt = license.UpdatedAt,
            CurrentAssignments = license.LicenseAssignments
                .Where(a => a.Status == "Active")
                .Select(a => new AssignmentResponseDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeCode = a.Employee.EmployeeCode,
                    EmployeeName = a.Employee.EmployeeName,
                    EmployeeEmail = a.Employee.Email,
                    Department = a.Employee.Department,
                    LicenseId = license.Id,
                    LicenseCode = license.LicenseCode,
                    SoftwareName = license.SoftwareName,
                    LicenseType = license.LicenseType,
                    Vendor = license.Vendor,
                    AssignDate = a.AssignDate,
                    ExpiryDate = a.ExpiryDate,
                    Status = a.Status,
                    Remarks = a.Remarks,
                    AssignedByUserName = a.AssignedByUser?.Email
                }).OrderByDescending(a => a.AssignDate).ToList(),
            RenewalHistory = license.Renewals
                .Select(r => new RenewalDto
                {
                    Id = r.Id,
                    LicenseId = r.LicenseId,
                    LicenseCode = license.LicenseCode,
                    SoftwareName = license.SoftwareName,
                    Vendor = license.Vendor,
                    RenewalDate = r.RenewalDate,
                    RenewalAmount = r.RenewalAmount,
                    PreviousExpiryDate = r.PreviousExpiryDate,
                    NewExpiryDate = r.NewExpiryDate,
                    Status = r.Status,
                    Remarks = r.Remarks,
                    RenewedByUserName = r.RenewedByUser?.Email,
                    CreatedAt = r.CreatedAt
                }).OrderByDescending(r => r.RenewalDate).ToList()
        };
    }

    public async Task<LicenseDto> CreateLicenseAsync(CreateLicenseDto request, int? currentUserId)
    {
        var existingCode = await _context.Licenses.AnyAsync(l => l.LicenseCode.ToLower() == request.LicenseCode.Trim().ToLower());
        if (existingCode)
        {
            throw new InvalidOperationException($"License code '{request.LicenseCode}' is already in use.");
        }

        var license = new LicenseMaster
        {
            LicenseCode = request.LicenseCode.Trim().ToUpper(),
            SoftwareName = request.SoftwareName.Trim(),
            LicenseType = request.LicenseType.Trim(),
            Vendor = request.Vendor.Trim(),
            PurchaseDate = request.PurchaseDate.ToUniversalTime(),
            ExpiryDate = request.ExpiryDate.ToUniversalTime(),
            TotalLicenses = request.TotalLicenses,
            AssignedCount = 0,
            AvailableCount = request.TotalLicenses,
            Cost = request.Cost,
            BillingCycle = request.BillingCycle.Trim(),
            RenewalCost = request.RenewalCost,
            LicenseReference = request.LicenseReference?.Trim(),
            Status = request.Status,
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "CREATE",
            "LICENSES",
            "LicenseMaster",
            license.Id,
            $"Added new software license '{license.SoftwareName}' ({license.LicenseCode}) with {license.TotalLicenses} total seats."
        );

        return new LicenseDto
        {
            Id = license.Id,
            LicenseCode = license.LicenseCode,
            SoftwareName = license.SoftwareName,
            LicenseType = license.LicenseType,
            Vendor = license.Vendor,
            PurchaseDate = license.PurchaseDate,
            ExpiryDate = license.ExpiryDate,
            TotalLicenses = license.TotalLicenses,
            AssignedCount = license.AssignedCount,
            AvailableCount = license.AvailableCount,
            Cost = license.Cost,
            BillingCycle = license.BillingCycle,
            RenewalCost = license.RenewalCost,
            LicenseReference = license.LicenseReference,
            Status = license.Status,
            Notes = license.Notes,
            CreatedAt = license.CreatedAt,
            UpdatedAt = license.UpdatedAt
        };
    }

    public async Task<LicenseDto> UpdateLicenseAsync(int id, UpdateLicenseDto request, int? currentUserId)
    {
        var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == id);
        if (license == null)
        {
            throw new KeyNotFoundException($"License with ID {id} not found.");
        }

        if (request.TotalLicenses < license.AssignedCount)
        {
            throw new InvalidOperationException($"Total licenses ({request.TotalLicenses}) cannot be less than currently assigned licenses ({license.AssignedCount}).");
        }

        license.SoftwareName = request.SoftwareName.Trim();
        license.LicenseType = request.LicenseType.Trim();
        license.Vendor = request.Vendor.Trim();
        license.PurchaseDate = request.PurchaseDate.ToUniversalTime();
        license.ExpiryDate = request.ExpiryDate.ToUniversalTime();
        license.TotalLicenses = request.TotalLicenses;
        license.AvailableCount = request.TotalLicenses - license.AssignedCount;
        license.Cost = request.Cost;
        license.BillingCycle = request.BillingCycle.Trim();
        license.RenewalCost = request.RenewalCost;
        license.LicenseReference = request.LicenseReference?.Trim();
        license.Status = request.Status;
        license.Notes = request.Notes?.Trim();
        license.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "UPDATE",
            "LICENSES",
            "LicenseMaster",
            license.Id,
            $"Updated license '{license.SoftwareName}' ({license.LicenseCode}). Total seats: {license.TotalLicenses}, Available: {license.AvailableCount}."
        );

        return new LicenseDto
        {
            Id = license.Id,
            LicenseCode = license.LicenseCode,
            SoftwareName = license.SoftwareName,
            LicenseType = license.LicenseType,
            Vendor = license.Vendor,
            PurchaseDate = license.PurchaseDate,
            ExpiryDate = license.ExpiryDate,
            TotalLicenses = license.TotalLicenses,
            AssignedCount = license.AssignedCount,
            AvailableCount = license.AvailableCount,
            Cost = license.Cost,
            BillingCycle = license.BillingCycle,
            RenewalCost = license.RenewalCost,
            LicenseReference = license.LicenseReference,
            Status = license.Status,
            Notes = license.Notes,
            CreatedAt = license.CreatedAt,
            UpdatedAt = license.UpdatedAt
        };
    }

    public async Task<bool> DeleteLicenseAsync(int id, int? currentUserId)
    {
        var license = await _context.Licenses
            .Include(l => l.LicenseAssignments)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (license == null)
        {
            throw new KeyNotFoundException($"License with ID {id} not found.");
        }

        var hasActiveAssignments = license.LicenseAssignments.Any(a => a.Status == "Active");
        if (hasActiveAssignments)
        {
            throw new InvalidOperationException("Cannot delete license that is currently assigned to employees. Please revoke all active assignments first.");
        }

        _context.Licenses.Remove(license);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "DELETE",
            "LICENSES",
            "LicenseMaster",
            id,
            $"Deleted software license '{license.SoftwareName}' ({license.LicenseCode})."
        );

        return true;
    }

    public async Task<List<string>> GetVendorsAsync()
    {
        return await _context.Licenses
            .Select(l => l.Vendor)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();
    }

    public async Task<List<string>> GetLicenseTypesAsync()
    {
        return await _context.Licenses
            .Select(l => l.LicenseType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }
}
