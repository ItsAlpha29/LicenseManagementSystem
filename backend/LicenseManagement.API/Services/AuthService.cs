using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LicenseManagement.API.Data;
using LicenseManagement.API.DTOs;
using LicenseManagement.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LicenseManagement.API.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null);
    Task<UserProfileDto> GetCurrentUserAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto request);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, IAuditService auditService)
    {
        _context = context;
        _configuration = configuration;
        _auditService = auditService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials or account is inactive.");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSecret = _configuration["JWT_SECRET"] ?? _configuration["Jwt:Secret"] ?? "DefaultEnterpriseSuperSecretKey32CharsMin!";
        var key = Encoding.UTF8.GetBytes(jwtSecret);
        var issuer = _configuration["JWT_ISSUER"] ?? _configuration["Jwt:Issuer"] ?? "LicenseManagementSystem";
        var audience = _configuration["JWT_AUDIENCE"] ?? _configuration["Jwt:Audience"] ?? "LicenseManagementUsers";
        var expiryMinutesStr = _configuration["JWT_EXPIRY_MINUTES"] ?? _configuration["Jwt:ExpiryMinutes"] ?? "480";
        var expiryMinutes = int.TryParse(expiryMinutesStr, out var exp) ? exp : 480;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("EmployeeId", user.EmployeeId?.ToString() ?? string.Empty),
                new Claim("Name", user.Employee?.EmployeeName ?? user.Email)
            }),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        await _auditService.LogAsync(
            user.Id,
            "LOGIN",
            "AUTH",
            "User",
            user.Id,
            $"User {user.Email} ({user.Role}) logged in successfully.",
            ipAddress
        );

        var profile = MapToProfileDto(user);

        return new LoginResponseDto
        {
            Token = tokenString,
            User = profile,
            ExpiresAt = tokenDescriptor.Expires.Value
        };
    }

    public async Task<UserProfileDto> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return MapToProfileDto(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto request)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (user.Employee != null && !string.IsNullOrWhiteSpace(request.Phone))
        {
            user.Employee.Phone = request.Phone.Trim();
            user.Employee.UpdatedAt = DateTime.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new InvalidOperationException("Current password verification failed.");
            }

            if (request.NewPassword.Length < 6)
            {
                throw new InvalidOperationException("New password must be at least 6 characters long.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _auditService.LogAsync(
                user.Id,
                "PASSWORD_CHANGE",
                "AUTH",
                "User",
                user.Id,
                $"User {user.Email} updated their password."
            );
        }

        await _context.SaveChangesAsync();
        return MapToProfileDto(user);
    }

    private static UserProfileDto MapToProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            EmployeeId = user.EmployeeId,
            Email = user.Email,
            Role = user.Role,
            EmployeeCode = user.Employee?.EmployeeCode,
            Name = user.Employee?.EmployeeName ?? user.Email,
            Department = user.Employee?.Department,
            Designation = user.Employee?.Designation,
            Phone = user.Employee?.Phone,
            LastLoginAt = user.LastLoginAt
        };
    }
}
