using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string module, string entityType, int? entityId, string remarks, string? ipAddress = null, string? metadata = null);
    Task<PagedResult<AuditHistoryDto>> GetAuditLogsAsync(string? module, string? action, int? userId, DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 20);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? userId, string action, string module, string entityType, int? entityId, string remarks, string? ipAddress = null, string? metadata = null)
    {
        var audit = new AuditHistory
        {
            UserId = userId,
            Action = action,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            DateTime = DateTime.UtcNow,
            Remarks = remarks,
            IpAddress = ipAddress,
            Metadata = metadata
        };

        _context.AuditHistories.Add(audit);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditHistoryDto>> GetAuditLogsAsync(string? module, string? action, int? userId, DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 20)
    {
        var query = _context.AuditHistories
            .Include(a => a.User)
            .ThenInclude(u => u!.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(a => a.Module.ToLower() == module.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.ToLower() == action.ToLower());
        }

        if (userId.HasValue && userId > 0)
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.DateTime >= startDate.Value.ToUniversalTime());
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.DateTime <= endDate.Value.ToUniversalTime());
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.DateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditHistoryDto
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = a.User != null ? (a.User.Employee != null ? a.User.Employee.EmployeeName : a.User.Email) : "System",
                UserEmail = a.User != null ? a.User.Email : "system@enterprise.com",
                UserRole = a.User != null ? a.User.Role : "SYSTEM",
                Action = a.Action,
                Module = a.Module,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                DateTime = a.DateTime,
                Remarks = a.Remarks,
                IpAddress = a.IpAddress,
                Metadata = a.Metadata
            })
            .ToListAsync();

        return new PagedResult<AuditHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
