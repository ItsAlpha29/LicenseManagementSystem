using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using LicenseManagement.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LicenseManagement.Tests;

public class AssignmentServiceTests
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
    public async Task AssignLicense_ShouldSucceed_AndCorrectlyUpdateSeatCounts()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();
        var mockNotif = new Mock<INotificationService>();

        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "EMP001",
            EmployeeName = "Kritika Chouhan",
            Email = "kritika@enterprise.com",
            Department = "IT",
            Designation = "Lead",
            Status = "Active"
        };

        var license = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-MS365",
            SoftwareName = "Microsoft 365",
            Vendor = "Microsoft",
            TotalLicenses = 10,
            AssignedCount = 0,
            AvailableCount = 10,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = "Active"
        };

        context.Employees.Add(employee);
        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        var service = new AssignmentService(context, mockAudit.Object, mockNotif.Object);

        // Act
        var result = await service.AssignLicenseAsync(new AssignLicenseDto
        {
            EmployeeId = employee.Id,
            LicenseId = license.Id,
            Remarks = "Standard setup"
        }, currentUserId: 99);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Active", result.Status);

        var updatedLicense = await context.Licenses.FindAsync(license.Id);
        Assert.NotNull(updatedLicense);
        Assert.Equal(1, updatedLicense.AssignedCount); // Rule 5: AssignedCount increases by 1
        Assert.Equal(9, updatedLicense.AvailableCount); // Rule 5: AvailableCount decreases by 1
    }

    [Fact]
    public async Task AssignLicense_WhenNoSeatsAvailable_ShouldThrowInvalidOperationException()
    {
        // Arrange (Rule 1: AvailableCount <= 0 cannot be assigned)
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();
        var mockNotif = new Mock<INotificationService>();

        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "EMP001",
            EmployeeName = "Kritika Chouhan",
            Email = "kritika@enterprise.com",
            Department = "IT",
            Designation = "Lead",
            Status = "Active"
        };

        var license = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-FULL",
            SoftwareName = "Photoshop",
            Vendor = "Adobe",
            TotalLicenses = 5,
            AssignedCount = 5,
            AvailableCount = 0, // 0 seats
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = "Active"
        };

        context.Employees.Add(employee);
        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        var service = new AssignmentService(context, mockAudit.Object, mockNotif.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignLicenseAsync(new AssignLicenseDto
            {
                EmployeeId = employee.Id,
                LicenseId = license.Id
            }, 1));

        Assert.Contains("No seats available", ex.Message);
    }

    [Fact]
    public async Task AssignLicense_WhenLicenseIsExpired_ShouldThrowInvalidOperationException()
    {
        // Arrange (Rule 2: Expired license cannot be assigned)
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();
        var mockNotif = new Mock<INotificationService>();

        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "EMP001",
            EmployeeName = "Kritika Chouhan",
            Email = "kritika@enterprise.com",
            Department = "IT",
            Designation = "Lead",
            Status = "Active"
        };

        var license = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-EXP",
            SoftwareName = "Old Software",
            Vendor = "Vendor",
            TotalLicenses = 10,
            AssignedCount = 2,
            AvailableCount = 8,
            ExpiryDate = DateTime.UtcNow.AddDays(-10), // Expired 10 days ago
            Status = "Expired"
        };

        context.Employees.Add(employee);
        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        var service = new AssignmentService(context, mockAudit.Object, mockNotif.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignLicenseAsync(new AssignLicenseDto
            {
                EmployeeId = employee.Id,
                LicenseId = license.Id
            }, 1));

        Assert.Contains("expired", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignLicense_WhenEmployeeIsInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange (Rule 3: Inactive employee cannot receive new license)
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();
        var mockNotif = new Mock<INotificationService>();

        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "EMP001",
            EmployeeName = "Inactive User",
            Email = "inactive@enterprise.com",
            Department = "IT",
            Designation = "Former Staff",
            Status = "Inactive" // Inactive
        };

        var license = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-MS365",
            SoftwareName = "Microsoft 365",
            Vendor = "Microsoft",
            TotalLicenses = 10,
            AssignedCount = 0,
            AvailableCount = 10,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = "Active"
        };

        context.Employees.Add(employee);
        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        var service = new AssignmentService(context, mockAudit.Object, mockNotif.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignLicenseAsync(new AssignLicenseDto
            {
                EmployeeId = employee.Id,
                LicenseId = license.Id
            }, 1));

        Assert.Contains("inactive employee", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignLicense_WhenDuplicateActiveAssignmentExists_ShouldThrowInvalidOperationException()
    {
        // Arrange (Rule 4: Same active license cannot be assigned twice)
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();
        var mockNotif = new Mock<INotificationService>();

        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "EMP001",
            EmployeeName = "Kritika Chouhan",
            Email = "kritika@enterprise.com",
            Department = "IT",
            Designation = "Lead",
            Status = "Active"
        };

        var license = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-MS365",
            SoftwareName = "Microsoft 365",
            Vendor = "Microsoft",
            TotalLicenses = 10,
            AssignedCount = 1,
            AvailableCount = 9,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = "Active"
        };

        var existingAssignment = new LicenseAssignment
        {
            Id = 1,
            EmployeeId = employee.Id,
            LicenseId = license.Id,
            AssignDate = DateTime.UtcNow.AddDays(-30),
            ExpiryDate = license.ExpiryDate,
            Status = "Active"
        };

        context.Employees.Add(employee);
        context.Licenses.Add(license);
        context.LicenseAssignments.Add(existingAssignment);
        await context.SaveChangesAsync();

        var service = new AssignmentService(context, mockAudit.Object, mockNotif.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignLicenseAsync(new AssignLicenseDto
            {
                EmployeeId = employee.Id,
                LicenseId = license.Id
            }, 1));

        Assert.Contains("already has an active assignment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RevokeLicense_ShouldSucceed_AndRestoreSeatCounts()
    {
        // Arrange (Rule 6: Revoking restores available seat count)
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();
        var mockNotif = new Mock<INotificationService>();

        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "EMP001",
            EmployeeName = "Kritika Chouhan",
            Email = "kritika@enterprise.com",
            Department = "IT",
            Designation = "Lead",
            Status = "Active"
        };

        var license = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-MS365",
            SoftwareName = "Microsoft 365",
            Vendor = "Microsoft",
            TotalLicenses = 10,
            AssignedCount = 5,
            AvailableCount = 5,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            Status = "Active"
        };

        var assignment = new LicenseAssignment
        {
            Id = 10,
            EmployeeId = employee.Id,
            LicenseId = license.Id,
            AssignDate = DateTime.UtcNow.AddDays(-20),
            ExpiryDate = license.ExpiryDate,
            Status = "Active"
        };

        context.Employees.Add(employee);
        context.Licenses.Add(license);
        context.LicenseAssignments.Add(assignment);
        await context.SaveChangesAsync();

        var service = new AssignmentService(context, mockAudit.Object, mockNotif.Object);

        // Act
        var result = await service.RevokeLicenseAsync(assignment.Id, new RevokeLicenseDto
        {
            RevocationReason = "License Reallocation",
            Remarks = "Project completed"
        }, currentUserId: 99);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Revoked", result.Status);
        Assert.Equal("License Reallocation", result.RevocationReason);

        var updatedLicense = await context.Licenses.FindAsync(license.Id);
        Assert.NotNull(updatedLicense);
        Assert.Equal(4, updatedLicense.AssignedCount); // Decreases by 1
        Assert.Equal(6, updatedLicense.AvailableCount); // Increases by 1
    }
}
