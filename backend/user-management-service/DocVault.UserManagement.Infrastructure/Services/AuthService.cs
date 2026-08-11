using DocVault.UserManagement.Application.DTOs.Auth;
using DocVault.UserManagement.Application.DTOs.Users;
using DocVault.UserManagement.Application.Interfaces;
using DocVault.UserManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DocVault.UserManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserManagementDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        UserManagementDbContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
    }

    // Login
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return null;

        var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValid)
            return null;

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        var memberships = await GetMembershipsAsync(user.Id);

        var token = GenerateJwtToken(user, isAdmin, memberships);
        var expiry = double.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!);

        return new LoginResponseDto
        {
            Token = token,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            IsAdmin = isAdmin,
            Projects = memberships,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiry)
        };
    }

    // Get Current User
    public async Task<CurrentUserDto?> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        var memberships = await GetMembershipsAsync(user.Id);

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            IsAdmin = isAdmin,
            Projects = memberships,
            IsActive = user.IsActive
        };
    }

    private async Task<List<ProjectMembershipDto>> GetMembershipsAsync(string userId)
    {
        var rows = await _context.UserProjects
            .Where(up => up.UserId == userId && up.IsActive)
            .ToListAsync();

        return rows.Select(up => new ProjectMembershipDto
        {
            ProjectId = up.ProjectId,
            ProjectName = up.ProjectName,
            Role = up.Role
        }).ToList();
    }

    // Generate JWT Token — one "project:{projectId}" claim per active membership,
    // holding that membership's role. Admin is global, not project-scoped.
    private string GenerateJwtToken(ApplicationUser user, bool isAdmin, List<ProjectMembershipDto> memberships)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var expiry = double.Parse(jwtSettings["ExpiryInMinutes"]!);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName",  user.LastName)
        };

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            claims.Add(new Claim("role", "Admin"));
        }

        // Distinct roles held across all projects — lets [Authorize(Roles="ProjectHead")]
        // keep working for someone who is ProjectHead in one project and User in another.
        foreach (var role in memberships.Select(m => m.Role).Distinct())
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        // One claim per project membership: "project:{projectId}" -> role in that project.
        foreach (var m in memberships)
        {
            claims.Add(new Claim($"project:{m.ProjectId}", m.Role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}