using DocVault.DocumentKnowledgeManagement.Application.DTOs.Auth;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Services;

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
        // Find User
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return null;

        // Validate Password
        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
            return null;

        // Get Role
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        // Generate Token
        var token = GenerateJwtToken(user, role);
        var expiresAt = DateTime.UtcNow.AddMinutes(
            double.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!));

        return new LoginResponseDto
        {
            Token = token,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = role,
            ProjectId = user.ProjectId,
            ExpiresAt = expiresAt
        };
    }

    
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
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiry = double.Parse(jwtSettings["ExpiryInMinutes"]!);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role,               role),
            new Claim("projectId",                   user.ProjectId?.ToString() ?? string.Empty),
            new Claim("firstName",                   user.FirstName),
            new Claim("lastName",                    user.LastName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}