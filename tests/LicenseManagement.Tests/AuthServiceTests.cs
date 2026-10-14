using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using LicenseManagement.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LicenseManagement.Tests;

public class AuthServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnJwtTokenAndUserProfile()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JWT_SECRET", "SuperSecretEnterpriseTestingKeyAtLeast32CharsLong!" },
            { "JWT_ISSUER", "LicenseManagementSystem" },
            { "JWT_AUDIENCE", "LicenseManagementUsers" },
            { "JWT_EXPIRY_MINUTES", "480" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var plainPassword = "Admin@123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        var employee = new Employee
        {
            Id = 1,
            EmployeeCode = "EMP001",
            EmployeeName = "Kritika Chouhan",
            Email = "admin@enterprise.com",
            Department = "IT",
            Designation = "CIO",
            Status = "Active"
        };

        var user = new User
        {
            Id = 1,
            EmployeeId = employee.Id,
            Email = "admin@enterprise.com",
            PasswordHash = passwordHash,
            Role = "ADMIN",
            IsActive = true
        };

        context.Employees.Add(employee);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthService(context, configuration, mockAudit.Object);

        // Act
        var result = await service.LoginAsync(new LoginRequestDto
        {
            Email = "admin@enterprise.com",
            Password = plainPassword
        });

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal("ADMIN", result.User.Role);
        Assert.Equal("admin@enterprise.com", result.User.Email);
        Assert.Equal("Kritika Chouhan", result.User.Name);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var mockAudit = new Mock<IAuditService>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JWT_SECRET", "SuperSecretEnterpriseTestingKeyAtLeast32CharsLong!" }
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var user = new User
        {
            Id = 1,
            Email = "admin@enterprise.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@123"),
            Role = "ADMIN",
            IsActive = true
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthService(context, configuration, mockAudit.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequestDto
            {
                Email = "admin@enterprise.com",
                Password = "WrongPassword!999"
            }));
    }
}
