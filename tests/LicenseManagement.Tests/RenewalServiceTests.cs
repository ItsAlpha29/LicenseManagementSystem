using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using LicenseManagement.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LicenseManagement.Tests;

public class RenewalServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ProcessRenewal_ShouldSucceed_AndExtendExpiryDate()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();
        var mockNotif = new Mock<INotificationService>();

        var originalExpiry = DateTime.UtcNow.AddMonths(1);
        var license = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-ADOBE",
            SoftwareName = "Adobe CC",
            Vendor = "Adobe",
            TotalLicenses = 10,
            AssignedCount = 5,
            AvailableCount = 5,
            Cost = 5000,
            RenewalCost = 5000,
            ExpiryDate = originalExpiry,
            Status = "ExpiringSoon"
        };

        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        var service = new RenewalService(context, mockAudit.Object, mockNotif.Object);
        var newExpiry = originalExpiry.AddYears(1);

        // Act
        var result = await service.ProcessRenewalAsync(new CreateRenewalDto
        {
            LicenseId = license.Id,
            RenewalAmount = 5200,
            NewExpiryDate = newExpiry,
            Remarks = "Annual corporate extension"
        }, currentUserId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(5200, result.RenewalAmount);

        var updatedLicense = await context.Licenses.FindAsync(license.Id);
        Assert.NotNull(updatedLicense);
        Assert.Equal(newExpiry.Date, updatedLicense.ExpiryDate.Date);
        Assert.Equal("Active", updatedLicense.Status); // Status restored to Active
    }

    [Fact]
    public async Task ProcessRenewal_WhenNewExpiryIsBeforeCurrentExpiry_ShouldThrowInvalidOperationException()
    {
        // Arrange (Rule 10: Renewal NewExpiryDate must be after current expiry date)
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();
        var mockNotif = new Mock<INotificationService>();

        var originalExpiry = DateTime.UtcNow.AddMonths(6);
        var license = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-TEST",
            SoftwareName = "Test App",
            Vendor = "Vendor",
            TotalLicenses = 10,
            AssignedCount = 0,
            AvailableCount = 10,
            ExpiryDate = originalExpiry,
            Status = "Active"
        };

        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        var service = new RenewalService(context, mockAudit.Object, mockNotif.Object);

        // Act & Assert (new expiry is before current expiry)
        var invalidNewExpiry = originalExpiry.AddDays(-10);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessRenewalAsync(new CreateRenewalDto
            {
                LicenseId = license.Id,
                RenewalAmount = 1000,
                NewExpiryDate = invalidNewExpiry
            }, 1));

        Assert.Contains("must be after current license expiry date", ex.Message);
    }
}
