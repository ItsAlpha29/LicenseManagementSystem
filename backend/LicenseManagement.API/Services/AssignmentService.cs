using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface IAssignmentService
{
    Task<PagedResult<AssignmentResponseDto>> GetAssignmentsAsync(string? search, string? department, string? status, int? employeeId, int? licenseId, int pageNumber = 1, int pageSize = 10);
    Task<AssignmentResponseDto> GetAssignmentByIdAsync(int id);
    Task<AssignmentResponseDto> AssignLicenseAsync(AssignLicenseDto request, int? currentUserId);
    Task<AssignmentResponseDto> RevokeLicenseAsync(int assignmentId, RevokeLicenseDto request, int? currentUserId);
    Task<List<AssignmentResponseDto>> GetMyLicensesAsync(int employeeId);
}

public class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public AssignmentService(ApplicationDbContext context, IAuditService auditService, INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<PagedResult<AssignmentResponseDto>> GetAssignmentsAsync(string? search, string? department, string? status, int? employeeId, int? licenseId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.LicenseAssignments
            .Include(a => a.Employee)
            .Include(a => a.License)
            .Include(a => a.AssignedByUser)
            .Include(a => a.RevokedByUser)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a =>
                a.Employee.EmployeeName.ToLower().Contains(s) ||
                a.Employee.EmployeeCode.ToLower().Contains(s) ||
                a.License.SoftwareName.ToLower().Contains(s) ||
                a.License.LicenseCode.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(department) && department != "All")
        {
            query = query.Where(a => a.Employee.Department.ToLower() == department.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(a => a.Status.ToLower() == status.Trim().ToLower());
        }

        if (employeeId.HasValue && employeeId > 0)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (licenseId.HasValue && licenseId > 0)
        {
            query = query.Where(a => a.LicenseId == licenseId.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.AssignDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AssignmentResponseDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeCode = a.Employee.EmployeeCode,
                EmployeeName = a.Employee.EmployeeName,
                EmployeeEmail = a.Employee.Email,
                Department = a.Employee.Department,
                LicenseId = a.LicenseId,
                LicenseCode = a.License.LicenseCode,
                SoftwareName = a.License.SoftwareName,
                LicenseType = a.License.LicenseType,
                Vendor = a.License.Vendor,
                AssignDate = a.AssignDate,
                ExpiryDate = a.ExpiryDate,
                AssignedByUserId = a.AssignedByUserId,
                AssignedByUserName = a.AssignedByUser != null ? a.AssignedByUser.Email : null,
                RevokedDate = a.RevokedDate,
                RevokedByUserId = a.RevokedByUserId,
                RevokedByUserName = a.RevokedByUser != null ? a.RevokedByUser.Email : null,
                RevocationReason = a.RevocationReason,
                Status = a.Status,
                Remarks = a.Remarks,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<AssignmentResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<AssignmentResponseDto> GetAssignmentByIdAsync(int id)
    {
        var assignment = await _context.LicenseAssignments
            .Include(a => a.Employee)
            .Include(a => a.License)
            .Include(a => a.AssignedByUser)
            .Include(a => a.RevokedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
        {
            throw new KeyNotFoundException($"Assignment with ID {id} not found.");
        }

        return new AssignmentResponseDto
        {
            Id = assignment.Id,
            EmployeeId = assignment.EmployeeId,
            EmployeeCode = assignment.Employee.EmployeeCode,
            EmployeeName = assignment.Employee.EmployeeName,
            EmployeeEmail = assignment.Employee.Email,
            Department = assignment.Employee.Department,
            LicenseId = assignment.LicenseId,
            LicenseCode = assignment.License.LicenseCode,
            SoftwareName = assignment.License.SoftwareName,
            LicenseType = assignment.License.LicenseType,
            Vendor = assignment.License.Vendor,
            AssignDate = assignment.AssignDate,
            ExpiryDate = assignment.ExpiryDate,
            AssignedByUserId = assignment.AssignedByUserId,
            AssignedByUserName = assignment.AssignedByUser?.Email,
            RevokedDate = assignment.RevokedDate,
            RevokedByUserId = assignment.RevokedByUserId,
            RevokedByUserName = assignment.RevokedByUser?.Email,
            RevocationReason = assignment.RevocationReason,
            Status = assignment.Status,
            Remarks = assignment.Remarks,
            CreatedAt = assignment.CreatedAt
        };
    }

    public async Task<AssignmentResponseDto> AssignLicenseAsync(AssignLicenseDto request, int? currentUserId)
    {
        // Use DB Transaction to ensure atomic license count updates and business rule integrity
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Fetch Employee & check status (Rule 3)
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

            if (employee == null)
            {
                throw new KeyNotFoundException($"Employee with ID {request.EmployeeId} not found.");
            }

            if (employee.Status != "Active")
            {
                throw new InvalidOperationException($"Cannot assign license to inactive employee '{employee.EmployeeName}' (Status: {employee.Status}).");
            }

            // Fetch License & check availability and expiry (Rules 1, 2, 7, 8)
            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Id == request.LicenseId);

            if (license == null)
            {
                throw new KeyNotFoundException($"License with ID {request.LicenseId} not found.");
            }

            if (license.ExpiryDate < DateTime.UtcNow.Date)
            {
                throw new InvalidOperationException($"License '{license.SoftwareName}' expired on {license.ExpiryDate:yyyy-MM-dd} and cannot be assigned.");
            }

            if (license.AvailableCount <= 0)
            {
                throw new InvalidOperationException($"No seats available for license '{license.SoftwareName}'. Total: {license.TotalLicenses}, Assigned: {license.AssignedCount}.");
            }

            // Check Duplicate Active Assignment (Rule 4)
            var duplicate = await _context.LicenseAssignments
                .AnyAsync(a => a.EmployeeId == request.EmployeeId && a.LicenseId == request.LicenseId && a.Status == "Active");

            if (duplicate)
            {
                throw new InvalidOperationException($"Employee '{employee.EmployeeName}' already has an active assignment of '{license.SoftwareName}'.");
            }

            // Create assignment
            var expiry = request.ExpiryDate ?? license.ExpiryDate;
            var assignment = new LicenseAssignment
            {
                EmployeeId = employee.Id,
                LicenseId = license.Id,
                AssignDate = DateTime.UtcNow,
                ExpiryDate = expiry.ToUniversalTime(),
                AssignedByUserId = currentUserId,
                Status = "Active",
                Remarks = request.Remarks?.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.LicenseAssignments.Add(assignment);

            // Update License Counts (Rules 5, 7, 8)
            license.AssignedCount += 1;
            license.AvailableCount = license.TotalLicenses - license.AssignedCount;
            license.UpdatedAt = DateTime.UtcNow;

            if (license.AvailableCount < 0)
            {
                throw new InvalidOperationException("License available count cannot become negative.");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Log Audit
            await _auditService.LogAsync(
                currentUserId,
                "ASSIGN",
                "ASSIGNMENTS",
                "LicenseAssignment",
                assignment.Id,
                $"Assigned license '{license.SoftwareName}' ({license.LicenseCode}) to {employee.EmployeeName} ({employee.EmployeeCode})."
            );

            // Create Notification
            await _notificationService.CreateNotificationAsync(
                employee.User?.Id,
                "License Assigned",
                $"You have been assigned a license for {license.SoftwareName}. Valid until {expiry:MMM dd, yyyy}.",
                "SUCCESS",
                "ASSIGNMENT",
                "/my-licenses"
            );

            return new AssignmentResponseDto
            {
                Id = assignment.Id,
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = employee.EmployeeName,
                EmployeeEmail = employee.Email,
                Department = employee.Department,
                LicenseId = license.Id,
                LicenseCode = license.LicenseCode,
                SoftwareName = license.SoftwareName,
                LicenseType = license.LicenseType,
                Vendor = license.Vendor,
                AssignDate = assignment.AssignDate,
                ExpiryDate = assignment.ExpiryDate,
                AssignedByUserId = currentUserId,
                Status = assignment.Status,
                Remarks = assignment.Remarks,
                CreatedAt = assignment.CreatedAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AssignmentResponseDto> RevokeLicenseAsync(int assignmentId, RevokeLicenseDto request, int? currentUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var assignment = await _context.LicenseAssignments
                .Include(a => a.Employee)
                .Include(a => a.License)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                throw new KeyNotFoundException($"Assignment with ID {assignmentId} not found.");
            }

            if (assignment.Status != "Active")
            {
                throw new InvalidOperationException($"Assignment is already '{assignment.Status}' and cannot be revoked.");
            }

            // Update assignment record (Rule 13: status tracking, do not delete)
            assignment.Status = "Revoked";
            assignment.RevokedDate = DateTime.UtcNow;
            assignment.RevokedByUserId = currentUserId;
            assignment.RevocationReason = request.RevocationReason.Trim();
            assignment.Remarks = request.Remarks?.Trim();
            assignment.UpdatedAt = DateTime.UtcNow;

            // Update License Counts (Rule 6)
            var license = assignment.License;
            if (license.AssignedCount > 0)
            {
                license.AssignedCount -= 1;
            }
            license.AvailableCount = license.TotalLicenses - license.AssignedCount;
            license.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Log Audit
            await _auditService.LogAsync(
                currentUserId,
                "REVOKE",
                "ASSIGNMENTS",
                "LicenseAssignment",
                assignment.Id,
                $"Revoked license '{license.SoftwareName}' from {assignment.Employee.EmployeeName} ({assignment.Employee.EmployeeCode}). Reason: {request.RevocationReason}."
            );

            // Create Notification
            await _notificationService.CreateNotificationAsync(
                assignment.Employee.User?.Id,
                "License Revoked",
                $"Your license for {license.SoftwareName} has been revoked. Reason: {request.RevocationReason}.",
                "INFO",
                "ASSIGNMENT",
                "/my-licenses"
            );

            return new AssignmentResponseDto
            {
                Id = assignment.Id,
                EmployeeId = assignment.EmployeeId,
                EmployeeCode = assignment.Employee.EmployeeCode,
                EmployeeName = assignment.Employee.EmployeeName,
                EmployeeEmail = assignment.Employee.Email,
                Department = assignment.Employee.Department,
                LicenseId = license.Id,
                LicenseCode = license.LicenseCode,
                SoftwareName = license.SoftwareName,
                LicenseType = license.LicenseType,
                Vendor = license.Vendor,
                AssignDate = assignment.AssignDate,
                ExpiryDate = assignment.ExpiryDate,
                RevokedDate = assignment.RevokedDate,
                RevokedByUserId = currentUserId,
                RevocationReason = assignment.RevocationReason,
                Status = assignment.Status,
                Remarks = assignment.Remarks,
                CreatedAt = assignment.CreatedAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<AssignmentResponseDto>> GetMyLicensesAsync(int employeeId)
    {
        return await _context.LicenseAssignments
            .Include(a => a.Employee)
            .Include(a => a.License)
            .Where(a => a.EmployeeId == employeeId && a.Status == "Active")
            .OrderBy(a => a.ExpiryDate)
            .Select(a => new AssignmentResponseDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeCode = a.Employee.EmployeeCode,
                EmployeeName = a.Employee.EmployeeName,
                EmployeeEmail = a.Employee.Email,
                Department = a.Employee.Department,
                LicenseId = a.LicenseId,
                LicenseCode = a.License.LicenseCode,
                SoftwareName = a.License.SoftwareName,
                LicenseType = a.License.LicenseType,
                Vendor = a.License.Vendor,
                AssignDate = a.AssignDate,
                ExpiryDate = a.ExpiryDate,
                Status = a.Status,
                Remarks = a.Remarks,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }
}
