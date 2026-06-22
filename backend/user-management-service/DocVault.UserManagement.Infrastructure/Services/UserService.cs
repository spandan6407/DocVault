using DocVault.UserManagement.Application.DTOs.Users;
using DocVault.UserManagement.Application.Events.Published;
using DocVault.UserManagement.Application.Interfaces;
using DocVault.UserManagement.Domain.Entities;
using DocVault.UserManagement.Infrastructure.Identity;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocVault.UserManagement.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserManagementDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public UserService(
        UserManager<ApplicationUser> userManager,
        UserManagementDbContext context,
        IPublishEndpoint publishEndpoint)
    {
        _userManager = userManager;
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    //  Create User
    public async Task<UserResponseDto?> CreateUserAsync(CreateUserDto request)
    {
        // Validate role
        if (request.Role == "Admin")
            return null;

        if (request.Role != "ProjectHead" && request.Role != "User")
            return null;

        // Check project exists in UserProjects
        var projectExists = await _context.UserProjects
            .AnyAsync(up => up.ProjectId == request.ProjectId
                         && up.IsActive);

        if (!projectExists)
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

        //  Publish Event to RabbitMQ
        await _publishEndpoint.Publish(new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = request.Role,
            ProjectId = request.ProjectId,
            CreatedAt = user.CreatedAt
        });

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
        Guid projectId,
        string requesterId,
        string requesterRole)
    {
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
        string userId,
        AssignProjectHeadDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        if (user.ProjectId != request.ProjectId)
            return null;

        // Find existing ProjectHead
        var existingHeads = await _userManager
            .GetUsersInRoleAsync("ProjectHead");
        var existingHead = existingHeads
            .FirstOrDefault(u => u.ProjectId == request.ProjectId);

        string? oldProjectHeadId = null;

        if (existingHead != null)
        {
            oldProjectHeadId = existingHead.Id;
            await _userManager.RemoveFromRoleAsync(
                existingHead, "ProjectHead");
            await _userManager.AddToRoleAsync(existingHead, "User");

            //  Publish Role Changed Event
            await _publishEndpoint.Publish(new UserRoleAssignedEvent
            {
                UserId = existingHead.Id,
                OldRole = "ProjectHead",
                NewRole = "User",
                ProjectId = request.ProjectId,
                AssignedAt = DateTime.UtcNow
            });
        }

        // Assign new ProjectHead
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, "ProjectHead");

        //  Publish ProjectHead Assigned Event
        await _publishEndpoint.Publish(new ProjectHeadAssignedEvent
        {
            NewProjectHeadId = user.Id,
            OldProjectHeadId = oldProjectHeadId,
            ProjectId = request.ProjectId,
            AssignedAt = DateTime.UtcNow
        });

        return await MapToResponseAsync(user);
    }

    //  Map to Response
    private async Task<UserResponseDto> MapToResponseAsync(
        ApplicationUser user)
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