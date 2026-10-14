using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LicenseManagement.API.Models;

[Table("LicenseMaster")]
public class LicenseMaster
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string LicenseCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string SoftwareName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LicenseType { get; set; } = "Subscription"; // Subscription, Perpetual, OpenSource, OEM

    [Required]
    [MaxLength(150)]
    public string Vendor { get; set; } = string.Empty;

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddYears(1);

    public int TotalLicenses { get; set; }

    public int AssignedCount { get; set; }

    public int AvailableCount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Cost { get; set; }

    [Required]
    [MaxLength(50)]
    public string BillingCycle { get; set; } = "Annual"; // Monthly, Annual, OneTime

    [Column(TypeName = "decimal(18,2)")]
    public decimal RenewalCost { get; set; }

    [MaxLength(255)]
    public string? LicenseReference { get; set; } // Masked/Hashed identifier, never plain license key

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, ExpiringSoon, Expired, Deprecated

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<LicenseAssignment> LicenseAssignments { get; set; } = new List<LicenseAssignment>();

    public virtual ICollection<Renewal> Renewals { get; set; } = new List<Renewal>();
}
