using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseManagement.API.Models;

[Table("Notifications")]
public class Notification
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; } // Null if broadcast to all/admins

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = "INFO"; // INFO, WARNING, CRITICAL, SUCCESS

    [MaxLength(50)]
    public string Category { get; set; } = "EXPIRY"; // EXPIRY, ASSIGNMENT, RENEWAL, SYSTEM

    public bool IsRead { get; set; } = false;

    public string? Link { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
