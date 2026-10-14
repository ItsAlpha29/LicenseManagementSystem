using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Services;

public interface INotificationService
{
    Task<NotificationSummaryDto> GetUserNotificationsAsync(int? userId, string userRole);
    Task<bool> MarkAsReadAsync(int notificationId, int? userId);
    Task<bool> MarkAllAsReadAsync(int? userId);
    Task CreateNotificationAsync(int? userId, string title, string message, string type, string category, string? link = null);
    Task RefreshExpiryNotificationsAsync();
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationSummaryDto> GetUserNotificationsAsync(int? userId, string userRole)
    {
        var now = DateTime.UtcNow.Date;

        // Auto-generate expiry alerts dynamically for admins and IT managers
        if (userRole == "ADMIN" || userRole == "IT_MANAGER")
        {
            var expiringCount = await _context.Licenses
                .CountAsync(l => l.ExpiryDate >= now && l.ExpiryDate <= now.AddDays(30) && l.Status != "Expired");

            var expiredCount = await _context.Licenses
                .CountAsync(l => l.ExpiryDate < now && l.Status != "Expired");

            if (expiringCount > 0 && !await _context.Notifications.AnyAsync(n => n.Category == "EXPIRY" && n.CreatedAt >= now.AddDays(-1)))
            {
                await CreateNotificationAsync(null, "Licenses Expiring Soon", $"{expiringCount} software license(s) are expiring within the next 30 days.", "WARNING", "EXPIRY", "/renewals");
            }
        }

        var query = _context.Notifications.AsNoTracking().AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(n => n.UserId == userId || n.UserId == null);
        }
        else
        {
            query = query.Where(n => n.UserId == null);
        }

        var totalUnread = await query.CountAsync(n => !n.IsRead);
        var criticalCount = await query.CountAsync(n => !n.IsRead && (n.Type == "CRITICAL" || n.Type == "WARNING"));

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(15)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Category = n.Category,
                IsRead = n.IsRead,
                Link = n.Link,
                CreatedAt = n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt)
            })
            .ToListAsync();

        return new NotificationSummaryDto
        {
            UnreadCount = totalUnread,
            CriticalCount = criticalCount,
            RecentNotifications = items
        };
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int? userId)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> MarkAllAsReadAsync(int? userId)
    {
        var notifications = await _context.Notifications
            .Where(n => !n.IsRead && (n.UserId == userId || (userId.HasValue && n.UserId == null)))
            .ToListAsync();

        foreach (var n in notifications)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task CreateNotificationAsync(int? userId, string title, string message, string type, string category, string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            Category = category,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task RefreshExpiryNotificationsAsync()
    {
        var now = DateTime.UtcNow.Date;
        var licenses = await _context.Licenses.ToListAsync();

        foreach (var license in licenses)
        {
            if (license.ExpiryDate < now && license.Status != "Expired")
            {
                license.Status = "Expired";
                license.UpdatedAt = DateTime.UtcNow;
            }
            else if (license.ExpiryDate <= now.AddDays(30) && license.ExpiryDate >= now && license.Status == "Active")
            {
                license.Status = "ExpiringSoon";
                license.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        if (timeSpan.TotalMinutes < 1) return "Just now";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
        return dateTime.ToString("MMM dd");
    }
}
