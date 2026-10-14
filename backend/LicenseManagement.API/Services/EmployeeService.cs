using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(string? search, string? department, string? role, string? status, int pageNumber = 1, int pageSize = 10, string? sortBy = "name", bool sortDesc = false);
    Task<EmployeeDetailDto> GetEmployeeByIdAsync(int id);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto request, int? currentUserId);
    Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto request, int? currentUserId);
    Task<EmployeeDto> UpdateStatusAsync(int id, string status, int? currentUserId);
    Task<bool> DeleteEmployeeAsync(int id, int? currentUserId);
    Task<List<string>> GetDepartmentsAsync();
}

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public EmployeeService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PagedResult<EmployeeDto>> GetEmployeesAsync(string? search, string? department, string? role, string? status, int pageNumber = 1, int pageSize = 10, string? sortBy = "name", bool sortDesc = false)
    {
        var query = _context.Employees
            .Include(e => e.Manager)
            .Include(e => e.LicenseAssignments)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e =>
                e.EmployeeName.ToLower().Contains(s) ||
                e.EmployeeCode.ToLower().Contains(s) ||
                e.Email.ToLower().Contains(s) ||
                e.Designation.ToLower().Contains(s) ||
                e.Department.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(department) && department != "All")
        {
            query = query.Where(e => e.Department.ToLower() == department.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(role) && role != "All")
        {
            query = query.Where(e => e.Role.ToLower() == role.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(e => e.Status.ToLower() == status.Trim().ToLower());
        }

        query = sortBy?.ToLower() switch
        {
            "code" => sortDesc ? query.OrderByDescending(e => e.EmployeeCode) : query.OrderBy(e => e.EmployeeCode),
            "department" => sortDesc ? query.OrderByDescending(e => e.Department) : query.OrderBy(e => e.Department),
            "role" => sortDesc ? query.OrderByDescending(e => e.Role) : query.OrderBy(e => e.Role),
            "joiningdate" => sortDesc ? query.OrderByDescending(e => e.JoiningDate) : query.OrderBy(e => e.JoiningDate),
            "status" => sortDesc ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            _ => sortDesc ? query.OrderByDescending(e => e.EmployeeName) : query.OrderBy(e => e.EmployeeName),
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                EmployeeName = e.EmployeeName,
                Email = e.Email,
                Phone = e.Phone,
                Department = e.Department,
                Designation = e.Designation,
                Role = e.Role,
                ManagerId = e.ManagerId,
                ManagerName = e.Manager != null ? e.Manager.EmployeeName : null,
                JoiningDate = e.JoiningDate,
                Status = e.Status,
                ActiveLicensesCount = e.LicenseAssignments.Count(a => a.Status == "Active"),
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<EmployeeDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeDetailDto> GetEmployeeByIdAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Manager)
            .Include(e => e.LicenseAssignments)
                .ThenInclude(a => a.License)
            .Include(e => e.LicenseAssignments)
                .ThenInclude(a => a.AssignedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {id} not found.");
        }

        var activityLogs = await _context.AuditHistories
            .Where(a => a.EntityType == "Employee" && a.EntityId == id)
            .OrderByDescending(a => a.DateTime)
            .Take(20)
            .Select(a => new AuditHistoryDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Action = a.Action,
                Module = a.Module,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                DateTime = a.DateTime,
                Remarks = a.Remarks
            })
            .ToListAsync();

        return new EmployeeDetailDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.EmployeeName,
            Email = employee.Email,
            Phone = employee.Phone,
            Department = employee.Department,
            Designation = employee.Designation,
            Role = employee.Role,
            ManagerId = employee.ManagerId,
            ManagerName = employee.Manager?.EmployeeName,
            JoiningDate = employee.JoiningDate,
            Status = employee.Status,
            ActiveLicensesCount = employee.LicenseAssignments.Count(a => a.Status == "Active"),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt,
            AssignedLicenses = employee.LicenseAssignments.Select(a => new AssignmentResponseDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = employee.EmployeeName,
                EmployeeEmail = employee.Email,
                Department = employee.Department,
                LicenseId = a.LicenseId,
                LicenseCode = a.License.LicenseCode,
                SoftwareName = a.License.SoftwareName,
                LicenseType = a.License.LicenseType,
                Vendor = a.License.Vendor,
                AssignDate = a.AssignDate,
                ExpiryDate = a.ExpiryDate,
                Status = a.Status,
                Remarks = a.Remarks,
                AssignedByUserName = a.AssignedByUser?.Email
            }).OrderByDescending(a => a.AssignDate).ToList(),
            ActivityLogs = activityLogs
        };
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto request, int? currentUserId)
    {
        var existingCode = await _context.Employees.AnyAsync(e => e.EmployeeCode.ToLower() == request.EmployeeCode.Trim().ToLower());
        if (existingCode)
        {
            throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' is already in use.");
        }

        var existingEmail = await _context.Employees.AnyAsync(e => e.Email.ToLower() == request.Email.Trim().ToLower());
        if (existingEmail)
        {
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");
        }

        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode.Trim().ToUpper(),
            EmployeeName = request.EmployeeName.Trim(),
            Email = request.Email.Trim().ToLower(),
            Phone = request.Phone?.Trim(),
            Department = request.Department.Trim(),
            Designation = request.Designation.Trim(),
            Role = request.Role,
            ManagerId = request.ManagerId,
            JoiningDate = request.JoiningDate.ToUniversalTime(),
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Create corresponding login user
        var defaultPassword = string.IsNullOrWhiteSpace(request.InitialPassword) ? "Employee@123" : request.InitialPassword;
        var user = new User
        {
            EmployeeId = employee.Id,
            Email = employee.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
            Role = employee.Role,
            IsActive = employee.Status == "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "CREATE",
            "EMPLOYEES",
            "Employee",
            employee.Id,
            $"Created employee {employee.EmployeeName} ({employee.EmployeeCode}) in department {employee.Department}."
        );

        return new EmployeeDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.EmployeeName,
            Email = employee.Email,
            Phone = employee.Phone,
            Department = employee.Department,
            Designation = employee.Designation,
            Role = employee.Role,
            ManagerId = employee.ManagerId,
            JoiningDate = employee.JoiningDate,
            Status = employee.Status,
            ActiveLicensesCount = 0,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto request, int? currentUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {id} not found.");
        }

        var oldRole = employee.Role;
        var oldStatus = employee.Status;

        employee.EmployeeName = request.EmployeeName.Trim();
        employee.Phone = request.Phone?.Trim();
        employee.Department = request.Department.Trim();
        employee.Designation = request.Designation.Trim();
        employee.Role = request.Role;
        employee.ManagerId = request.ManagerId;
        employee.JoiningDate = request.JoiningDate.ToUniversalTime();
        employee.Status = request.Status;
        employee.UpdatedAt = DateTime.UtcNow;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
        if (user != null)
        {
            user.Role = request.Role;
            user.IsActive = request.Status == "Active";
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var remarks = $"Updated employee {employee.EmployeeName} ({employee.EmployeeCode}).";
        if (oldRole != request.Role) remarks += $" Role changed from {oldRole} to {request.Role}.";
        if (oldStatus != request.Status) remarks += $" Status changed from {oldStatus} to {request.Status}.";

        await _auditService.LogAsync(
            currentUserId,
            "UPDATE",
            "EMPLOYEES",
            "Employee",
            employee.Id,
            remarks
        );

        return new EmployeeDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.EmployeeName,
            Email = employee.Email,
            Phone = employee.Phone,
            Department = employee.Department,
            Designation = employee.Designation,
            Role = employee.Role,
            ManagerId = employee.ManagerId,
            JoiningDate = employee.JoiningDate,
            Status = employee.Status,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<EmployeeDto> UpdateStatusAsync(int id, string status, int? currentUserId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {id} not found.");
        }

        employee.Status = status;
        employee.UpdatedAt = DateTime.UtcNow;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
        if (user != null)
        {
            user.IsActive = status == "Active";
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "STATUS_CHANGE",
            "EMPLOYEES",
            "Employee",
            employee.Id,
            $"Changed status of employee {employee.EmployeeName} ({employee.EmployeeCode}) to {status}."
        );

        return new EmployeeDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeName = employee.EmployeeName,
            Email = employee.Email,
            Phone = employee.Phone,
            Department = employee.Department,
            Designation = employee.Designation,
            Role = employee.Role,
            JoiningDate = employee.JoiningDate,
            Status = employee.Status,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<bool> DeleteEmployeeAsync(int id, int? currentUserId)
    {
        var employee = await _context.Employees
            .Include(e => e.LicenseAssignments)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {id} not found.");
        }

        var activeAssignments = employee.LicenseAssignments.Any(a => a.Status == "Active");
        if (activeAssignments)
        {
            throw new InvalidOperationException("Cannot delete employee with active assigned licenses. Please revoke licenses first or deactivate the employee.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
        if (user != null)
        {
            _context.Users.Remove(user);
        }

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            "DELETE",
            "EMPLOYEES",
            "Employee",
            id,
            $"Deleted employee {employee.EmployeeName} ({employee.EmployeeCode})."
        );

        return true;
    }

    public async Task<List<string>> GetDepartmentsAsync()
    {
        return await _context.Employees
            .Select(e => e.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }
}
