using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface IRenewalService
{
    Task<PagedResult<RenewalDto>> GetRenewalsAsync(string? search, int? licenseId, string? status, int pageNumber = 1, int pageSize = 10);
    Task<RenewalDto> GetRenewalByIdAsync(int id);
    Task<RenewalDto> ProcessRenewalAsync(CreateRenewalDto request, int? currentUserId);
}

public class RenewalService : IRenewalService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public RenewalService(ApplicationDbContext context, IAuditService auditService, INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<RenewalDto>> GetRenewalsAsync(string? search, int? licenseId, string? status, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Renewals
            .Include(r => r.License)
            .Include(r => r.RenewedByUser)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(r =>
                r.License.SoftwareName.ToLower().Contains(s) ||
                r.License.LicenseCode.ToLower().Contains(s) ||
                r.License.Vendor.ToLower().Contains(s));
        }

        if (licenseId.HasValue && licenseId > 0)
        {
            query = query.Where(r => r.LicenseId == licenseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(r => r.Status.ToLower() == status.Trim().ToLower());
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.RenewalDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RenewalDto
            {
                Id = r.Id,
                LicenseId = r.LicenseId,
                LicenseCode = r.License.LicenseCode,
                SoftwareName = r.License.SoftwareName,
                Vendor = r.License.Vendor,
                RenewalDate = r.RenewalDate,
                RenewalAmount = r.RenewalAmount,
                PreviousExpiryDate = r.PreviousExpiryDate,
                NewExpiryDate = r.NewExpiryDate,
                Status = r.Status,
                Remarks = r.Remarks,
                RenewedByUserId = r.RenewedByUserId,
                RenewedByUserName = r.RenewedByUser != null ? r.RenewedByUser.Email : "System",
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<RenewalDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<RenewalDto> GetRenewalByIdAsync(int id)
    {
        var renewal = await _context.Renewals
            .Include(r => r.License)
            .Include(r => r.RenewedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (renewal == null)
        {
            throw new KeyNotFoundException($"Renewal with ID {id} not found.");
        }

        return new RenewalDto
        {
            Id = renewal.Id,
            LicenseId = renewal.LicenseId,
            LicenseCode = renewal.License.LicenseCode,
            SoftwareName = renewal.License.SoftwareName,
            Vendor = renewal.License.Vendor,
            RenewalDate = renewal.RenewalDate,
            RenewalAmount = renewal.RenewalAmount,
            PreviousExpiryDate = renewal.PreviousExpiryDate,
            NewExpiryDate = renewal.NewExpiryDate,
            Status = renewal.Status,
            Remarks = renewal.Remarks,
            RenewedByUserId = renewal.RenewedByUserId,
            RenewedByUserName = renewal.RenewedByUser?.Email,
            CreatedAt = renewal.CreatedAt
        };
    }

    public async Task<RenewalDto> ProcessRenewalAsync(CreateRenewalDto request, int? currentUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == request.LicenseId);
            if (license == null)
            {
                throw new KeyNotFoundException($"License with ID {request.LicenseId} not found.");
            }

            var newExpiry = request.NewExpiryDate.ToUniversalTime();

            // Rule 10: Renewal NewExpiryDate must be after current expiry date
            if (newExpiry <= license.ExpiryDate)
            {
                throw new InvalidOperationException($"New expiry date ({newExpiry:yyyy-MM-dd}) must be after current license expiry date ({license.ExpiryDate:yyyy-MM-dd}).");
            }

            var previousExpiry = license.ExpiryDate;

            var renewal = new Renewal
            {
                LicenseId = license.Id,
                RenewalDate = DateTime.UtcNow,
                RenewalAmount = request.RenewalAmount,
                PreviousExpiryDate = previousExpiry,
                NewExpiryDate = newExpiry,
                Status = "Completed",
                Remarks = request.Remarks?.Trim(),
                RenewedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Renewals.Add(renewal);

            // Update License record
            license.ExpiryDate = newExpiry;
            license.RenewalCost = request.RenewalAmount;
            if (license.Status == "Expired" || license.Status == "ExpiringSoon")
            {
                license.Status = "Active";
            }
            license.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Log Audit
            await _auditService.LogAsync(
                currentUserId,
                "RENEW",
                "RENEWALS",
                "LicenseMaster",
                license.Id,
                $"Renewed license '{license.SoftwareName}' ({license.LicenseCode}) for amount ${request.RenewalAmount:N2}. Expiry extended from {previousExpiry:yyyy-MM-dd} to {newExpiry:yyyy-MM-dd}."
            );

            // Notification
            await _notificationService.CreateNotificationAsync(
                null,
                "License Renewed",
                $"{license.SoftwareName} was successfully renewed until {newExpiry:MMM dd, yyyy}.",
                "SUCCESS",
                "RENEWAL",
                "/renewals"
            );

            return new RenewalDto
            {
                Id = renewal.Id,
                LicenseId = license.Id,
                LicenseCode = license.LicenseCode,
                SoftwareName = license.SoftwareName,
                Vendor = license.Vendor,
                RenewalDate = renewal.RenewalDate,
                RenewalAmount = renewal.RenewalAmount,
                PreviousExpiryDate = previousExpiry,
                NewExpiryDate = newExpiry,
                Status = renewal.Status,
                Remarks = renewal.Remarks,
                RenewedByUserId = currentUserId,
                CreatedAt = renewal.CreatedAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
