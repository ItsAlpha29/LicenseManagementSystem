namespace LicenseManagement.API.DTOs;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserProfileDto User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public int? EmployeeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? Name { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UpdateProfileDto
{
    public string? Phone { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}
