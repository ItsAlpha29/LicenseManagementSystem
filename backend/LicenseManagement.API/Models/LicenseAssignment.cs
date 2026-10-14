using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseManagement.API.Models;

[Table("LicenseAssignments")]
public class LicenseAssignment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int LicenseId { get; set; }

    public DateTime AssignDate { get; set; } = DateTime.UtcNow;

    public DateTime ExpiryDate { get; set; }

    public int? AssignedByUserId { get; set; }

    public DateTime? RevokedDate { get; set; }

    public int? RevokedByUserId { get; set; }

    [MaxLength(255)]
    public string? RevocationReason { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Revoked, Expired

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(EmployeeId))]
    public virtual Employee Employee { get; set; } = null!;

    [ForeignKey(nameof(LicenseId))]
    public virtual LicenseMaster License { get; set; } = null!;

    [ForeignKey(nameof(AssignedByUserId))]
    public virtual User? AssignedByUser { get; set; }

    [ForeignKey(nameof(RevokedByUserId))]
    public virtual User? RevokedByUser { get; set; }
}
