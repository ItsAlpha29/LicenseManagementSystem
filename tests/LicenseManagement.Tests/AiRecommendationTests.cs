using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using LicenseManagement.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LicenseManagement.Tests;

public class AiRecommendationTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private AiService CreateAiService(ApplicationDbContext context)
    {
        var mockConfig = new Mock<IConfiguration>();
        var mockAsgn = new Mock<IAssignmentService>();
        var mockRen = new Mock<IRenewalService>();
        var mockLogger = new Mock<ILogger<AiService>>();
        var mockHttpFactory = new Mock<IHttpClientFactory>();
        mockHttpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        return new AiService(
            context,
            mockConfig.Object,
            mockAsgn.Object,
            mockRen.Object,
            mockLogger.Object,
            mockHttpFactory.Object
        );
    }

    [Fact]
    public async Task GetRecommendations_ForEngineeringArchitect_ShouldPrioritizeVisualStudioAndMicrosoft365()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var emp = new Employee
        {
            Id = 1,
            EmployeeCode = "EMP001",
            EmployeeName = "Rahul Verma",
            Department = "Engineering",
            Designation = "Senior Full Stack Architect",
            Status = "Active"
        };

        var licVS = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-VS2026",
            SoftwareName = "Visual Studio Enterprise",
            Vendor = "Microsoft",
            TotalLicenses = 20,
            AssignedCount = 5,
            AvailableCount = 15,
            Status = "Active"
        };

        var licAdobe = new LicenseMaster
        {
            Id = 2,
            LicenseCode = "LIC-ADOBECC",
            SoftwareName = "Adobe Creative Cloud All Apps",
            Vendor = "Adobe",
            TotalLicenses = 10,
            AssignedCount = 5,
            AvailableCount = 5,
            Status = "Active"
        };

        var licM365 = new LicenseMaster
        {
            Id = 3,
            LicenseCode = "LIC-MS365",
            SoftwareName = "Microsoft 365 Enterprise E5",
            Vendor = "Microsoft",
            TotalLicenses = 50,
            AssignedCount = 10,
            AvailableCount = 40,
            Status = "Active"
        };

        context.Employees.Add(emp);
        context.Licenses.AddRange(licVS, licAdobe, licM365);
        await context.SaveChangesAsync();

        var service = CreateAiService(context);

        // Act
        var result = await service.GetRecommendationsForEmployeeAsync(emp.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Engineering", result.Department);
        Assert.NotEmpty(result.Recommendations);

        // Visual Studio and Microsoft 365 should have higher match scores than Adobe CC for an engineer
        var topRec = result.Recommendations.First();
        Assert.True(topRec.SoftwareName.Contains("Visual Studio") || topRec.SoftwareName.Contains("Microsoft 365"));
        Assert.True(topRec.MatchScore >= 90);

        var adobeRec = result.Recommendations.First(r => r.LicenseId == licAdobe.Id);
        Assert.True(adobeRec.MatchScore < topRec.MatchScore);
    }

    [Fact]
    public async Task GetRecommendations_ForDesigner_ShouldPrioritizeAdobeCreativeCloud()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var emp = new Employee
        {
            Id = 2,
            EmployeeCode = "EMP002",
            EmployeeName = "Sneha Patel",
            Department = "Design & UX",
            Designation = "Lead Product Designer",
            Status = "Active"
        };

        var licVS = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-VS",
            SoftwareName = "Visual Studio Enterprise",
            Vendor = "Microsoft",
            TotalLicenses = 10,
            AssignedCount = 2,
            AvailableCount = 8,
            Status = "Active"
        };

        var licAdobe = new LicenseMaster
        {
            Id = 2,
            LicenseCode = "LIC-ADOBE",
            SoftwareName = "Adobe Creative Cloud",
            Vendor = "Adobe",
            TotalLicenses = 10,
            AssignedCount = 2,
            AvailableCount = 8,
            Status = "Active"
        };

        context.Employees.Add(emp);
        context.Licenses.AddRange(licVS, licAdobe);
        await context.SaveChangesAsync();

        var service = CreateAiService(context);

        // Act
        var result = await service.GetRecommendationsForEmployeeAsync(emp.Id);

        // Assert
        Assert.NotNull(result);
        var adobeRec = result.Recommendations.First(r => r.LicenseId == licAdobe.Id);
        var vsRec = result.Recommendations.First(r => r.LicenseId == licVS.Id);

        Assert.True(adobeRec.MatchScore > vsRec.MatchScore);
        Assert.Equal("HIGH", adobeRec.Priority);
    }

    [Fact]
    public async Task CheckDuplicateLicense_WhenExactSoftwareExists_ShouldReturn100PercentAndExactMatch()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var existingLic = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-ADOBE-01",
            SoftwareName = "Adobe Creative Cloud All Apps",
            Vendor = "Adobe Systems",
            TotalLicenses = 10,
            AssignedCount = 5,
            AvailableCount = 5,
            Status = "Active"
        };

        context.Licenses.Add(existingLic);
        await context.SaveChangesAsync();

        var service = CreateAiService(context);

        // Act - Request check with exact name (case insensitive)
        var result = await service.CheckDuplicateLicenseAsync(new DuplicateDetectionRequestDto
        {
            SoftwareName = "adobe creative cloud all apps",
            Vendor = "Adobe"
        });

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.Equal(100, result.HighestSimilarityPercentage);
        Assert.NotEmpty(result.Matches);
        Assert.Equal("EXACT", result.Matches.First().MatchLevel);
    }

    [Fact]
    public async Task CheckDuplicateLicense_WhenCloseFuzzyNameProvided_ShouldDetectHighSimilarity()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();

        var existingLic = new LicenseMaster
        {
            Id = 1,
            LicenseCode = "LIC-MS365",
            SoftwareName = "Microsoft 365 Enterprise E5",
            Vendor = "Microsoft",
            TotalLicenses = 50,
            AssignedCount = 20,
            AvailableCount = 30,
            Status = "Active"
        };

        context.Licenses.Add(existingLic);
        await context.SaveChangesAsync();

        var service = CreateAiService(context);

        // Act - Check with variant "Microsoft Office 365 E5"
        var result = await service.CheckDuplicateLicenseAsync(new DuplicateDetectionRequestDto
        {
            SoftwareName = "Microsoft 365 Pro Enterprise",
            Vendor = "Microsoft Corporation"
        });

        // Assert
        Assert.True(result.HasDuplicates);
        Assert.True(result.HighestSimilarityPercentage >= 70);
        Assert.NotEmpty(result.Matches);
    }
}
