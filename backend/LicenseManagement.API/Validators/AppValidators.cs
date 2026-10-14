using FluentValidation;
using LicenseManagement.API.DTOs;

namespace LicenseManagement.API.Validators;

public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeCode)
            .NotEmpty().WithMessage("Employee code is required.")
            .MaximumLength(50).WithMessage("Employee code cannot exceed 50 characters.");

        RuleFor(x => x.EmployeeName)
            .NotEmpty().WithMessage("Employee name is required.")
            .MaximumLength(150).WithMessage("Employee name cannot exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Department is required.")
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters.");

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(100).WithMessage("Designation cannot exceed 100 characters.");

        RuleFor(x => x.Role)
            .Must(r => r == "ADMIN" || r == "IT_MANAGER" || r == "EMPLOYEE")
            .WithMessage("Role must be ADMIN, IT_MANAGER, or EMPLOYEE.");
    }
}

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeName)
            .NotEmpty().WithMessage("Employee name is required.")
            .MaximumLength(150).WithMessage("Employee name cannot exceed 150 characters.");

        RuleFor(x => x.Department)
            .NotEmpty().WithMessage("Department is required.")
            .MaximumLength(100).WithMessage("Department cannot exceed 100 characters.");

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(100).WithMessage("Designation cannot exceed 100 characters.");

        RuleFor(x => x.Role)
            .Must(r => r == "ADMIN" || r == "IT_MANAGER" || r == "EMPLOYEE")
            .WithMessage("Role must be ADMIN, IT_MANAGER, or EMPLOYEE.");
    }
}

public class CreateLicenseValidator : AbstractValidator<CreateLicenseDto>
{
    public CreateLicenseValidator()
    {
        RuleFor(x => x.LicenseCode)
            .NotEmpty().WithMessage("License code is required.")
            .MaximumLength(50).WithMessage("License code cannot exceed 50 characters.");

        RuleFor(x => x.SoftwareName)
            .NotEmpty().WithMessage("Software name is required.")
            .MaximumLength(150).WithMessage("Software name cannot exceed 150 characters.");

        RuleFor(x => x.Vendor)
            .NotEmpty().WithMessage("Vendor is required.")
            .MaximumLength(150).WithMessage("Vendor cannot exceed 150 characters.");

        RuleFor(x => x.TotalLicenses)
            .GreaterThan(0).WithMessage("Total licenses must be at least 1.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.");

        RuleFor(x => x.RenewalCost)
            .GreaterThanOrEqualTo(0).WithMessage("Renewal cost cannot be negative.");

        // Rule 9: ExpiryDate cannot be before PurchaseDate
        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.PurchaseDate)
            .WithMessage("License expiry date must be after the purchase date.");
    }
}

public class UpdateLicenseValidator : AbstractValidator<UpdateLicenseDto>
{
    public UpdateLicenseValidator()
    {
        RuleFor(x => x.SoftwareName)
            .NotEmpty().WithMessage("Software name is required.")
            .MaximumLength(150).WithMessage("Software name cannot exceed 150 characters.");

        RuleFor(x => x.Vendor)
            .NotEmpty().WithMessage("Vendor is required.")
            .MaximumLength(150).WithMessage("Vendor cannot exceed 150 characters.");

        RuleFor(x => x.TotalLicenses)
            .GreaterThan(0).WithMessage("Total licenses must be at least 1.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.PurchaseDate)
            .WithMessage("License expiry date must be after the purchase date.");
    }
}

public class AssignLicenseValidator : AbstractValidator<AssignLicenseDto>
{
    public AssignLicenseValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("A valid employee must be selected.");

        RuleFor(x => x.LicenseId)
            .GreaterThan(0).WithMessage("A valid software license must be selected.");
    }
}

public class CreateRenewalValidator : AbstractValidator<CreateRenewalDto>
{
    public CreateRenewalValidator()
    {
        RuleFor(x => x.LicenseId)
            .GreaterThan(0).WithMessage("A valid license must be selected.");

        RuleFor(x => x.RenewalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Renewal amount cannot be negative.");

        RuleFor(x => x.NewExpiryDate)
            .GreaterThan(DateTime.UtcNow.Date).WithMessage("New expiry date must be in the future.");
    }
}
