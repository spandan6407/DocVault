using DocVault.UserManagement.Application.DTOs.Auth;
using DocVault.UserManagement.Application.Interfaces;
using DocVault.UserManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DocVault.UserManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    //  Login
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return null;

        var isValid = await _userManager
            .CheckPasswordAsync(user, request.Password);
        if (!isValid)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;
        var token = GenerateJwtToken(user, role);
        var expiry = double.Parse(
            _configuration["JwtSettings:ExpiryInMinutes"]!);

        return new LoginResponseDto
        {
            Token = token,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = role,
            ProjectId = user.ProjectId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiry)
        };
    }

    //  Get Current User
    public async Task<CurrentUserDto?> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = role,
            ProjectId = user.ProjectId,
            IsActive = user.IsActive
        };
    }

    //  Generate JWT Token
    private string GenerateJwtToken(ApplicationUser user, string role)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var expiry = double.Parse(jwtSettings["ExpiryInMinutes"]!);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Role,               role),
            // Also include the standard JWT "role" claim to ensure other services map roles correctly
            new("role",                       role),
            new("projectId", user.ProjectId?.ToString() ?? string.Empty),
            new("firstName", user.FirstName),
            new("lastName",  user.LastName)
        };

        var key = new SymmetricSecurityKey(
                              Encoding.UTF8.GetBytes(secretKey!));
        var credentials = new SigningCredentials(
                              key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}