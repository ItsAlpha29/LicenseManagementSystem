using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseManagement.API.Models;

[Table("Renewals")]
public class Renewal
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int LicenseId { get; set; }

    public DateTime RenewalDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal RenewalAmount { get; set; }

    public DateTime PreviousExpiryDate { get; set; }

    public DateTime NewExpiryDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Completed"; // Completed, Pending, Failed

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public int? RenewedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(LicenseId))]
    public virtual LicenseMaster License { get; set; } = null!;

    [ForeignKey(nameof(RenewedByUserId))]
    public virtual User? RenewedByUser { get; set; }
}
