using DocVault.DocumentKnowledgeManagement.Application.DTOs.Users;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    //  Create User
    public async Task<UserResponseDto?> CreateUserAsync(CreateUserDto request)
    {
        // BR-002: Only Admin can create users
        // BR-003: Every user must belong to exactly one project

        // Validate role — Admin cannot be assigned via API (BR-008)
        if (request.Role == "Admin")
            return null;

        // Validate role is valid
        if (request.Role != "ProjectHead" && request.Role != "User")
            return null;

        // Check project exists
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive);

        if (project == null)
            return null;

        // Create User
        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            ProjectId = request.ProjectId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return null;

        // Assign Role
        await _userManager.AddToRoleAsync(user, request.Role);

        return await MapToResponseAsync(user);
    }

    //  Get All Users
    public async Task<List<UserResponseDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserResponseDto>();
        foreach (var user in users)
            result.Add(await MapToResponseAsync(user));

        return result;
    }

    //  Get Users By Project
    public async Task<List<UserResponseDto>> GetUsersByProjectAsync(
        Guid projectId, string requesterId, string requesterRole)
    {
        // BR-011: ProjectHead can only view users from own project
        if (requesterRole == "ProjectHead")
        {
            var requester = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == requesterId);

            if (requester == null || requester.ProjectId != projectId)
                return new List<UserResponseDto>();
        }

        var users = await _context.Users
            .Where(u => u.ProjectId == projectId && u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserResponseDto>();
        foreach (var user in users)
            result.Add(await MapToResponseAsync(user));

        return result;
    }

    //  Assign Project Head
    public async Task<UserResponseDto?> AssignProjectHeadAsync(
        string userId, AssignProjectHeadDto request)
    {
        // BR-005: Only one ProjectHead per project
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        // Check user belongs to this project
        if (user.ProjectId != request.ProjectId)
            return null;

        // Find existing ProjectHead of this project
        var existingProjectHeads = await _userManager
            .GetUsersInRoleAsync("ProjectHead");

        var existingHead = existingProjectHeads
            .FirstOrDefault(u => u.ProjectId == request.ProjectId);

        if (existingHead != null)
        {
            // Remove ProjectHead role → assign User role
            await _userManager.RemoveFromRoleAsync(existingHead, "ProjectHead");
            await _userManager.AddToRoleAsync(existingHead, "User");
        }

        // Remove current role → assign ProjectHead role
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, "ProjectHead");

        return await MapToResponseAsync(user);
    }

    //  Map to Response
    private async Task<UserResponseDto> MapToResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email!,
            Role = role,
            ProjectId = user.ProjectId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}