namespace LicenseManagement.API.DTOs;

public class RenewalDto
{
    public int Id { get; set; }
    public int LicenseId { get; set; }
    public string LicenseCode { get; set; } = string.Empty;
    public string SoftwareName { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public DateTime RenewalDate { get; set; }
    public decimal RenewalAmount { get; set; }
    public DateTime PreviousExpiryDate { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public int? RenewedByUserId { get; set; }
    public string? RenewedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRenewalDto
{
    public int LicenseId { get; set; }
    public decimal RenewalAmount { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public string? Remarks { get; set; }
}
