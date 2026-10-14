namespace LicenseManagement.API.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "INFO"; // INFO, WARNING, CRITICAL, SUCCESS
    public string Category { get; set; } = "EXPIRY";
    public bool IsRead { get; set; }
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

public class NotificationSummaryDto
{
    public int UnreadCount { get; set; }
    public int CriticalCount { get; set; }
    public List<NotificationDto> RecentNotifications { get; set; } = new();
}
