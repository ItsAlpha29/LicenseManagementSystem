using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.API.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Apply migrations automatically if supported
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migrate notice/fallback: {ex.Message}");
            await context.Database.EnsureCreatedAsync();
        }

        if (await context.Users.AnyAsync())
        {
            return; // DB has already been seeded
        }

        Console.WriteLine("🌱 Seeding Enterprise License Management System demo data...");

        // 1. Seed Employees
        var empKritika = new Employee
        {
            EmployeeCode = "EMP001",
            EmployeeName = "Kritika Chouhan",
            Email = "kritika@enterprise.com",
            Phone = "+1 (555) 234-5678",
            Department = "Information Technology",
            Designation = "Chief Information Officer",
            Role = "ADMIN",
            JoiningDate = DateTime.UtcNow.AddYears(-3),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddYears(-3),
            UpdatedAt = DateTime.UtcNow
        };

        var empPriyanka = new Employee
        {
            EmployeeCode = "EMP002",
            EmployeeName = "Priyanka Sharma",
            Email = "priyanka@enterprise.com",
            Phone = "+1 (555) 345-6789",
            Department = "Information Technology",
            Designation = "IT Asset & Operations Lead",
            Role = "IT_MANAGER",
            JoiningDate = DateTime.UtcNow.AddYears(-2),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow
        };

        var empRahul = new Employee
        {
            EmployeeCode = "EMP003",
            EmployeeName = "Rahul Verma",
            Email = "rahul@enterprise.com",
            Phone = "+1 (555) 456-7890",
            Department = "Engineering",
            Designation = "Senior Full Stack Architect",
            Role = "EMPLOYEE",
            JoiningDate = DateTime.UtcNow.AddYears(-1),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow
        };

        var empSneha = new Employee
        {
            EmployeeCode = "EMP004",
            EmployeeName = "Sneha Patel",
            Email = "sneha@enterprise.com",
            Phone = "+1 (555) 567-8901",
            Department = "Design & UX",
            Designation = "Lead Product Designer",
            Role = "EMPLOYEE",
            JoiningDate = DateTime.UtcNow.AddMonths(-18),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddMonths(-18),
            UpdatedAt = DateTime.UtcNow
        };

        var empAmit = new Employee
        {
            EmployeeCode = "EMP005",
            EmployeeName = "Amit Kumar",
            Email = "amit@enterprise.com",
            Phone = "+1 (555) 678-9012",
            Department = "Business Intelligence",
            Designation = "Data & Analytics Specialist",
            Role = "EMPLOYEE",
            JoiningDate = DateTime.UtcNow.AddMonths(-10),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddMonths(-10),
            UpdatedAt = DateTime.UtcNow
        };

        var empPooja = new Employee
        {
            EmployeeCode = "EMP006",
            EmployeeName = "Pooja Singh",
            Email = "pooja@enterprise.com",
            Phone = "+1 (555) 789-0123",
            Department = "Engineering",
            Designation = "CAD & Systems Engineer",
            Role = "EMPLOYEE",
            JoiningDate = DateTime.UtcNow.AddMonths(-8),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddMonths(-8),
            UpdatedAt = DateTime.UtcNow
        };

        var employees = new List<Employee> { empKritika, empPriyanka, empRahul, empSneha, empAmit, empPooja };
        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();

        // Establish reporting hierarchy
        empPriyanka.ManagerId = empKritika.Id;
        empRahul.ManagerId = empPriyanka.Id;
        empSneha.ManagerId = empPriyanka.Id;
        empAmit.ManagerId = empPriyanka.Id;
        empPooja.ManagerId = empPriyanka.Id;
        await context.SaveChangesAsync();

        // 2. Seed Users
        var users = new List<User>
        {
            new()
            {
                EmployeeId = empKritika.Id,
                Email = "admin@enterprise.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "ADMIN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                EmployeeId = empKritika.Id,
                Email = "kritika@enterprise.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "ADMIN",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                EmployeeId = empPriyanka.Id,
                Email = "itmanager@enterprise.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ItManager@123"),
                Role = "IT_MANAGER",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                EmployeeId = empPriyanka.Id,
                Email = "priyanka@enterprise.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ItManager@123"),
                Role = "IT_MANAGER",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                EmployeeId = empRahul.Id,
                Email = "rahul@enterprise.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                Role = "EMPLOYEE",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                EmployeeId = empSneha.Id,
                Email = "sneha@enterprise.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                Role = "EMPLOYEE",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                EmployeeId = empAmit.Id,
                Email = "amit@enterprise.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                Role = "EMPLOYEE",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                EmployeeId = empPooja.Id,
                Email = "pooja@enterprise.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                Role = "EMPLOYEE",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var adminUser = users[0];

        // 3. Seed Software Licenses
        var licM365 = new LicenseMaster
        {
            LicenseCode = "LIC-MS-365",
            SoftwareName = "Microsoft 365 Business Premium",
            LicenseType = "Subscription",
            Vendor = "Microsoft Corporation",
            PurchaseDate = DateTime.UtcNow.AddMonths(-6),
            ExpiryDate = DateTime.UtcNow.AddMonths(6),
            TotalLicenses = 50,
            AssignedCount = 6,
            AvailableCount = 44,
            Cost = 11000.00m,
            BillingCycle = "Annual",
            RenewalCost = 11000.00m,
            LicenseReference = "MSFT-PREM-****-9821",
            Status = "Active",
            Notes = "Standard cloud productivity and identity suite for all organizational personnel.",
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow
        };

        var licAdobe = new LicenseMaster
        {
            LicenseCode = "LIC-ADB-CC",
            SoftwareName = "Adobe Creative Cloud All Apps",
            LicenseType = "Subscription",
            Vendor = "Adobe Systems Inc.",
            PurchaseDate = DateTime.UtcNow.AddDays(-345),
            ExpiryDate = DateTime.UtcNow.AddDays(20), // Expiring in 20 days
            TotalLicenses = 10,
            AssignedCount = 2,
            AvailableCount = 8,
            Cost = 8400.00m,
            BillingCycle = "Annual",
            RenewalCost = 8400.00m,
            LicenseReference = "ADBE-CC-****-4412",
            Status = "ExpiringSoon",
            Notes = "Design suite for Creative & UI/UX teams. Renewal pending approval.",
            CreatedAt = DateTime.UtcNow.AddDays(-345),
            UpdatedAt = DateTime.UtcNow
        };

        var licPowerBI = new LicenseMaster
        {
            LicenseCode = "LIC-MS-PBI",
            SoftwareName = "Power BI Pro",
            LicenseType = "Subscription",
            Vendor = "Microsoft Corporation",
            PurchaseDate = DateTime.UtcNow.AddMonths(-4),
            ExpiryDate = DateTime.UtcNow.AddMonths(8),
            TotalLicenses = 25,
            AssignedCount = 2,
            AvailableCount = 23,
            Cost = 3000.00m,
            BillingCycle = "Annual",
            RenewalCost = 3000.00m,
            LicenseReference = "MSFT-PBI-****-1109",
            Status = "Active",
            Notes = "Enterprise BI and data visualization tool for leadership and data specialists.",
            CreatedAt = DateTime.UtcNow.AddMonths(-4),
            UpdatedAt = DateTime.UtcNow
        };

        var licAutoCad = new LicenseMaster
        {
            LicenseCode = "LIC-DSK-CAD",
            SoftwareName = "AutoCAD Architecture & Design",
            LicenseType = "Perpetual",
            Vendor = "Autodesk Inc.",
            PurchaseDate = DateTime.UtcNow.AddMonths(-10),
            ExpiryDate = DateTime.UtcNow.AddMonths(2),
            TotalLicenses = 8,
            AssignedCount = 1,
            AvailableCount = 7,
            Cost = 14800.00m,
            BillingCycle = "Annual",
            RenewalCost = 3200.00m,
            LicenseReference = "ADSK-CAD-****-7734",
            Status = "Active",
            Notes = "CAD engineering workstation software for infrastructure planning.",
            CreatedAt = DateTime.UtcNow.AddMonths(-10),
            UpdatedAt = DateTime.UtcNow
        };

        var licVS = new LicenseMaster
        {
            LicenseCode = "LIC-MS-VSPRO",
            SoftwareName = "Visual Studio Professional 2026",
            LicenseType = "Subscription",
            Vendor = "Microsoft Corporation",
            PurchaseDate = DateTime.UtcNow.AddMonths(-2),
            ExpiryDate = DateTime.UtcNow.AddMonths(10),
            TotalLicenses = 20,
            AssignedCount = 2,
            AvailableCount = 18,
            Cost = 9000.00m,
            BillingCycle = "Annual",
            RenewalCost = 9000.00m,
            LicenseReference = "MSFT-VS-****-3329",
            Status = "Active",
            Notes = "Integrated IDE developer licensing for backend and engineering.",
            CreatedAt = DateTime.UtcNow.AddMonths(-2),
            UpdatedAt = DateTime.UtcNow
        };

        var licCanva = new LicenseMaster
        {
            LicenseCode = "LIC-CNV-PRO",
            SoftwareName = "Canva Pro Enterprise",
            LicenseType = "Subscription",
            Vendor = "Canva Pty Ltd",
            PurchaseDate = DateTime.UtcNow.AddDays(-350),
            ExpiryDate = DateTime.UtcNow.AddDays(15), // Expiring in 15 days
            TotalLicenses = 15,
            AssignedCount = 2,
            AvailableCount = 13,
            Cost = 1800.00m,
            BillingCycle = "Annual",
            RenewalCost = 1800.00m,
            LicenseReference = "CNVA-ENT-****-8821",
            Status = "ExpiringSoon",
            Notes = "Brand asset and marketing collateral design platform.",
            CreatedAt = DateTime.UtcNow.AddDays(-350),
            UpdatedAt = DateTime.UtcNow
        };

        var licZoom = new LicenseMaster
        {
            LicenseCode = "LIC-ZM-ENT",
            SoftwareName = "Zoom Pro Enterprise",
            LicenseType = "Subscription",
            Vendor = "Zoom Video Communications",
            PurchaseDate = DateTime.UtcNow.AddMonths(-5),
            ExpiryDate = DateTime.UtcNow.AddMonths(7),
            TotalLicenses = 40,
            AssignedCount = 5,
            AvailableCount = 35,
            Cost = 6000.00m,
            BillingCycle = "Annual",
            RenewalCost = 6000.00m,
            LicenseReference = "ZM-ENT-****-5510",
            Status = "Active",
            Notes = "Video conferencing and virtual townhall accounts.",
            CreatedAt = DateTime.UtcNow.AddMonths(-5),
            UpdatedAt = DateTime.UtcNow
        };

        var licenses = new List<LicenseMaster> { licM365, licAdobe, licPowerBI, licAutoCad, licVS, licCanva, licZoom };
        context.Licenses.AddRange(licenses);
        await context.SaveChangesAsync();

        // 4. Seed Assignments
        var assignments = new List<LicenseAssignment>
        {
            // M365 to all employees
            new() { EmployeeId = empKritika.Id, LicenseId = licM365.Id, AssignDate = DateTime.UtcNow.AddMonths(-5), ExpiryDate = licM365.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empPriyanka.Id, LicenseId = licM365.Id, AssignDate = DateTime.UtcNow.AddMonths(-5), ExpiryDate = licM365.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empRahul.Id, LicenseId = licM365.Id, AssignDate = DateTime.UtcNow.AddMonths(-4), ExpiryDate = licM365.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empSneha.Id, LicenseId = licM365.Id, AssignDate = DateTime.UtcNow.AddMonths(-4), ExpiryDate = licM365.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empAmit.Id, LicenseId = licM365.Id, AssignDate = DateTime.UtcNow.AddMonths(-3), ExpiryDate = licM365.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empPooja.Id, LicenseId = licM365.Id, AssignDate = DateTime.UtcNow.AddMonths(-2), ExpiryDate = licM365.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },

            // Adobe CC to Designer and PM
            new() { EmployeeId = empSneha.Id, LicenseId = licAdobe.Id, AssignDate = DateTime.UtcNow.AddMonths(-8), ExpiryDate = licAdobe.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empRahul.Id, LicenseId = licAdobe.Id, AssignDate = DateTime.UtcNow.AddMonths(-6), ExpiryDate = licAdobe.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },

            // Power BI to CIO & Data Specialist
            new() { EmployeeId = empKritika.Id, LicenseId = licPowerBI.Id, AssignDate = DateTime.UtcNow.AddMonths(-3), ExpiryDate = licPowerBI.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empAmit.Id, LicenseId = licPowerBI.Id, AssignDate = DateTime.UtcNow.AddMonths(-3), ExpiryDate = licPowerBI.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },

            // AutoCAD to Pooja
            new() { EmployeeId = empPooja.Id, LicenseId = licAutoCad.Id, AssignDate = DateTime.UtcNow.AddMonths(-6), ExpiryDate = licAutoCad.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },

            // Visual Studio to Rahul and Priyanka
            new() { EmployeeId = empRahul.Id, LicenseId = licVS.Id, AssignDate = DateTime.UtcNow.AddMonths(-2), ExpiryDate = licVS.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empPriyanka.Id, LicenseId = licVS.Id, AssignDate = DateTime.UtcNow.AddMonths(-2), ExpiryDate = licVS.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },

            // Canva to Sneha and Amit
            new() { EmployeeId = empSneha.Id, LicenseId = licCanva.Id, AssignDate = DateTime.UtcNow.AddMonths(-6), ExpiryDate = licCanva.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empAmit.Id, LicenseId = licCanva.Id, AssignDate = DateTime.UtcNow.AddMonths(-4), ExpiryDate = licCanva.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },

            // Zoom Pro
            new() { EmployeeId = empKritika.Id, LicenseId = licZoom.Id, AssignDate = DateTime.UtcNow.AddMonths(-4), ExpiryDate = licZoom.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empPriyanka.Id, LicenseId = licZoom.Id, AssignDate = DateTime.UtcNow.AddMonths(-4), ExpiryDate = licZoom.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empRahul.Id, LicenseId = licZoom.Id, AssignDate = DateTime.UtcNow.AddMonths(-3), ExpiryDate = licZoom.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empSneha.Id, LicenseId = licZoom.Id, AssignDate = DateTime.UtcNow.AddMonths(-3), ExpiryDate = licZoom.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" },
            new() { EmployeeId = empAmit.Id, LicenseId = licZoom.Id, AssignDate = DateTime.UtcNow.AddMonths(-3), ExpiryDate = licZoom.ExpiryDate, AssignedByUserId = adminUser.Id, Status = "Active" }
        };

        context.LicenseAssignments.AddRange(assignments);
        await context.SaveChangesAsync();

        // 5. Seed Past Renewal History
        var renewals = new List<Renewal>
        {
            new()
            {
                LicenseId = licM365.Id,
                RenewalDate = DateTime.UtcNow.AddMonths(-6),
                RenewalAmount = 11000.00m,
                PreviousExpiryDate = DateTime.UtcNow.AddMonths(-6),
                NewExpiryDate = DateTime.UtcNow.AddMonths(6),
                Status = "Completed",
                Remarks = "Annual corporate license renewal",
                RenewedByUserId = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new()
            {
                LicenseId = licZoom.Id,
                RenewalDate = DateTime.UtcNow.AddMonths(-5),
                RenewalAmount = 6000.00m,
                PreviousExpiryDate = DateTime.UtcNow.AddMonths(-5),
                NewExpiryDate = DateTime.UtcNow.AddMonths(7),
                Status = "Completed",
                Remarks = "Enterprise expansion renewal with 40 seats",
                RenewedByUserId = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-5)
            }
        };

        context.Renewals.AddRange(renewals);
        await context.SaveChangesAsync();

        // 6. Seed Audit History Records
        var audits = new List<AuditHistory>
        {
            new() { UserId = adminUser.Id, Action = "CREATE", Module = "EMPLOYEES", EntityType = "Employee", EntityId = empKritika.Id, DateTime = DateTime.UtcNow.AddDays(-10), Remarks = "Initialized administrative user Kritika Chouhan (CIO)." },
            new() { UserId = adminUser.Id, Action = "CREATE", Module = "LICENSES", EntityType = "LicenseMaster", EntityId = licM365.Id, DateTime = DateTime.UtcNow.AddDays(-9), Remarks = "Provisioned Microsoft 365 Business Premium (50 seats)." },
            new() { UserId = adminUser.Id, Action = "CREATE", Module = "LICENSES", EntityType = "LicenseMaster", EntityId = licAdobe.Id, DateTime = DateTime.UtcNow.AddDays(-8), Remarks = "Provisioned Adobe Creative Cloud All Apps (10 seats)." },
            new() { UserId = adminUser.Id, Action = "ASSIGN", Module = "ASSIGNMENTS", EntityType = "LicenseAssignment", EntityId = assignments[0].Id, DateTime = DateTime.UtcNow.AddDays(-7), Remarks = "Assigned Microsoft 365 to Kritika Chouhan." },
            new() { UserId = adminUser.Id, Action = "ASSIGN", Module = "ASSIGNMENTS", EntityType = "LicenseAssignment", EntityId = assignments[6].Id, DateTime = DateTime.UtcNow.AddDays(-6), Remarks = "Assigned Adobe Creative Cloud to Sneha Patel." },
            new() { UserId = adminUser.Id, Action = "ASSIGN", Module = "ASSIGNMENTS", EntityType = "LicenseAssignment", EntityId = assignments[11].Id, DateTime = DateTime.UtcNow.AddDays(-3), Remarks = "Assigned Visual Studio Professional to Rahul Verma." },
            new() { UserId = adminUser.Id, Action = "LOGIN", Module = "AUTH", EntityType = "User", EntityId = adminUser.Id, DateTime = DateTime.UtcNow.AddHours(-1), Remarks = "Admin logged in via Web UI." }
        };

        context.AuditHistories.AddRange(audits);

        // 7. Seed Initial Notifications
        var notifications = new List<Notification>
        {
            new() { Title = "Adobe Creative Cloud Expiring Soon", Message = "Adobe Creative Cloud All Apps is expiring in 20 days. Renewal required.", Type = "WARNING", Category = "EXPIRY", Link = "/renewals", IsRead = false, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new() { Title = "Canva Pro Expiring Soon", Message = "Canva Pro Enterprise expires in 15 days on " + licCanva.ExpiryDate.ToString("MMM dd, yyyy"), Type = "WARNING", Category = "EXPIRY", Link = "/renewals", IsRead = false, CreatedAt = DateTime.UtcNow.AddHours(-5) },
            new() { Title = "Welcome to License Management System", Message = "System initialized with .NET 10 Web API and PostgreSQL database.", Type = "SUCCESS", Category = "SYSTEM", IsRead = true, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        Console.WriteLine("✅ Database seeding completed successfully!");
    }
}
