namespace LicenseManagement.API.DTOs;

public class AuditHistoryDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public DateTime DateTime { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Metadata { get; set; }
}
