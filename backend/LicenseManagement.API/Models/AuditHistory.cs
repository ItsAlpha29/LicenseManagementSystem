using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseManagement.API.Models;

[Table("AuditHistory")]
public class AuditHistory
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // LOGIN, ASSIGN, REVOKE, CREATE, UPDATE, DELETE, RENEW, STATUS_CHANGE

    [Required]
    [MaxLength(100)]
    public string Module { get; set; } = string.Empty; // AUTH, EMPLOYEES, LICENSES, ASSIGNMENTS, RENEWALS, AI

    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public DateTime DateTime { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(1000)]
    public string Remarks { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(4000)]
    public string? Metadata { get; set; } // JSON formatted additional metadata

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
