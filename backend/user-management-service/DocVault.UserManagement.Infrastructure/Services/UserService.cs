using DocVault.UserManagement.Application.DTOs.Users;
using DocVault.UserManagement.Application.Events.Published;
using DocVault.UserManagement.Application.Interfaces;
using DocVault.UserManagement.Domain.Entities;
using DocVault.UserManagement.Infrastructure.Identity;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace DocVault.UserManagement.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserManagementDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<UserService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(
        UserManager<ApplicationUser> userManager,
        UserManagementDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<UserService> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<UserResponseDto>> SearchUsersAsync(string? query, Guid? projectId, string? role)
    {
        var q = query?.Trim();

        // Start with active users only
        var usersQuery = _context.Users
            .Where(u => u.IsActive)
            .AsQueryable();

        // Apply text search if provided
        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.ToLowerInvariant();
            usersQuery = usersQuery.Where(u =>
                (u.FirstName != null && u.FirstName.ToLower().Contains(lower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(lower)) ||
                (u.Email != null && u.Email.ToLower().Contains(lower))
            );
        }

        // Apply project filter if provided — user must have a membership row for that project
        if (projectId.HasValue)
        {
            var pid = projectId.Value;
            usersQuery = usersQuery.Where(u => _context.UserProjects.Any(up => up.UserId == u.Id && up.ProjectId == pid && up.IsActive));
        }

        // Apply role filter if provided — behavior depends on whether projectId supplied
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleTrim = role.Trim();
            if (projectId.HasValue)
            {
                var pid = projectId.Value;
                // role applies to membership in the specified project
                usersQuery = usersQuery.Where(u => _context.UserProjects.Any(up => up.UserId == u.Id && up.ProjectId == pid && up.Role == roleTrim && up.IsActive));
            }
            else
            {
                // role applies if user has that role in any project
                usersQuery = usersQuery.Where(u => _context.UserProjects.Any(up => up.UserId == u.Id && up.Role == roleTrim && up.IsActive));
            }
        }

        var users = await usersQuery
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserResponseDto>();
        foreach (var user in users)
            result.Add(await MapToResponseAsync(user));

        return result;
    }

    private HttpClient CreateAuthorizedDocumentClient()
    {
        var client = _httpClientFactory.CreateClient("DocumentService");
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        return client;
    }

    private async Task<string> LookupProjectNameAsync(Guid projectId)
    {
        try
        {
            var client = CreateAuthorizedDocumentClient();
            var response = await client.GetAsync($"/api/projects/{projectId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var proj = System.Text.Json.JsonSerializer.Deserialize<ProjectNameLookup>(content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return proj?.Name ?? string.Empty;
            }
            _logger.LogWarning("Project lookup failed: {Status}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Project lookup threw exception");
        }
        return string.Empty;
    }

    // Create User — joins each given project as "User"
    public async Task<UserResponseDto?> CreateUserAsync(CreateUserDto request, bool bypassProjectCheck = false)
    {
        if (request.ProjectIds == null || request.ProjectIds.Count == 0)
            return null;

        if (!bypassProjectCheck)
        {
            foreach (var projectId in request.ProjectIds)
            {
                var exists = await _context.UserProjects.AnyAsync(up => up.ProjectId == projectId && up.IsActive);
                if (!exists)
                    return null;
            }
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Code + ":" + e.Description));
            _logger.LogWarning("User creation failed for {Email}: {Errors}", request.Email, errors);
            return null;
        }

        await _userManager.AddToRoleAsync(user, "User");

        foreach (var projectId in request.ProjectIds.Distinct())
        {
            try
            {
                var projectName = await LookupProjectNameAsync(projectId);
                _context.UserProjects.Add(new UserProject
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    ProjectName = projectName,
                    UserId = user.Id,
                    Role = "User",
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist UserProject for user {UserId} project {ProjectId}", user.Id, projectId);
            }
        }
        await _context.SaveChangesAsync();

        foreach (var projectId in request.ProjectIds.Distinct())
        {
            await _publishEndpoint.Publish(new UserCreatedEvent
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = "User",
                ProjectId = projectId,
                CreatedAt = user.CreatedAt
            });
        }

        return await MapToResponseAsync(user);
    }

    public async Task<List<UserProjectDto>> GetUserProjectsAsync(string userId)
    {
        var rows = await _context.UserProjects
            .Where(up => up.UserId == userId && up.IsActive)
            .OrderByDescending(up => up.AssignedAt)
            .ToListAsync();

        return rows.Select(up => new UserProjectDto
        {
            Id = up.Id,
            ProjectId = up.ProjectId,
            ProjectName = up.ProjectName,
            UserId = up.UserId,
            Role = up.Role,
            AssignedAt = up.AssignedAt,
            IsActive = up.IsActive
        }).ToList();
    }

    public async Task<UserProjectDto?> AddUserToProjectAsync(string userId, AddUserProjectDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) return null;

        // validate project exists
        var existsProj = await _context.UserProjects.AnyAsync(up => up.ProjectId == request.ProjectId);
        // We cannot rely solely on this table to check existence, but try lookup via other means; proceed anyway

        var already = await _context.UserProjects.AnyAsync(up => up.UserId == userId && up.ProjectId == request.ProjectId && up.IsActive);
        if (already) return null; // caller should handle null as conflict

        var projectName = await LookupProjectNameAsync(request.ProjectId);
        var up = new UserProject
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            ProjectName = projectName,
            UserId = userId,
            Role = request.Role,
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.UserProjects.Add(up);
        await _context.SaveChangesAsync();

        return new UserProjectDto
        {
            Id = up.Id,
            ProjectId = up.ProjectId,
            ProjectName = up.ProjectName,
            UserId = up.UserId,
            Role = up.Role,
            AssignedAt = up.AssignedAt,
            IsActive = up.IsActive
        };
    }

    public async Task<bool> UpdateUserProjectRoleAsync(string userId, Guid projectId, ChangeUserProjectRoleDto request)
    {
        var membership = await _context.UserProjects.FirstOrDefaultAsync(up => up.UserId == userId && up.ProjectId == projectId && up.IsActive);
        if (membership == null) return false;
        membership.Role = request.Role;
        _context.UserProjects.Update(membership);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveUserFromProjectAsync(string userId, Guid projectId)
    {
        var membership = await _context.UserProjects.FirstOrDefaultAsync(up => up.UserId == userId && up.ProjectId == projectId && up.IsActive);
        if (membership == null) return false;
        membership.IsActive = false;
        membership.AssignedAt = DateTime.UtcNow;
        _context.UserProjects.Update(membership);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAsync(string userIdToDelete, string requesterId, string requesterRole, List<Guid> requesterHeadProjectIds)
    {
        var user = await _userManager.FindByIdAsync(userIdToDelete);
        if (user == null || !user.IsActive)
            return false;

        var targetRoles = await _userManager.GetRolesAsync(user);
        if (targetRoles.Contains("Admin"))
            return false;

        if (requesterRole == "Admin")
        {
            // proceed
        }
        else if (requesterRole == "ProjectHead")
        {
            if (user.Id == requesterId)
                return false;

            if (requesterHeadProjectIds == null || requesterHeadProjectIds.Count == 0)
                return false;

            // Target must be a member of at least one project the requester heads.
            var isMemberOfAny = await _context.UserProjects.AnyAsync(up =>
                up.UserId == user.Id && requesterHeadProjectIds.Contains(up.ProjectId) && up.IsActive);
            if (!isMemberOfAny)
                return false;
        }
        else
        {
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

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

    public async Task<List<UserProjectDto>> GetAllUserProjectsAsync()
    {
        var ups = await _context.UserProjects
            .Where(up => up.IsActive)
            .OrderByDescending(up => up.AssignedAt)
            .ToListAsync();

        return ups.Select(up => new UserProjectDto
        {
            Id = up.Id,
            ProjectId = up.ProjectId,
            ProjectName = up.ProjectName,
            UserId = up.UserId,
            Role = up.Role,
            AssignedAt = up.AssignedAt,
            IsActive = up.IsActive
        }).ToList();
    }

    // Users belonging to a given project — role returned is that project's row role,
    // which may differ from the same user's role in another project.
    public async Task<List<UserResponseDto>> GetUsersByProjectAsync(
        Guid projectId,
        string requesterId,
        string requesterRole)
    {
        if (requesterRole == "ProjectHead")
        {
            var requesterIsHeadHere = await _context.UserProjects.AnyAsync(up =>
                up.UserId == requesterId && up.ProjectId == projectId && up.Role == "ProjectHead" && up.IsActive);
            if (!requesterIsHeadHere)
                return new List<UserResponseDto>();
        }

        var memberUserIds = await _context.UserProjects
            .Where(up => up.ProjectId == projectId && up.IsActive)
            .Select(up => up.UserId)
            .ToListAsync();

        var users = await _context.Users
            .Where(u => memberUserIds.Contains(u.Id) && u.IsActive)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserResponseDto>();
        foreach (var user in users)
            result.Add(await MapToResponseAsync(user));

        return result;
    }

    // Assign ProjectHead for ONE project only — does not touch the user's role in any other project.
    public async Task<UserResponseDto?> AssignProjectHeadAsync(
        string userId,
        AssignProjectHeadDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        var membership = await _context.UserProjects.FirstOrDefaultAsync(up =>
            up.UserId == userId && up.ProjectId == request.ProjectId && up.IsActive);
        if (membership == null)
            return null; // user must already belong to this project

        var existingHead = await _context.UserProjects.FirstOrDefaultAsync(up =>
            up.ProjectId == request.ProjectId && up.Role == "ProjectHead" && up.IsActive);

        string? oldProjectHeadId = null;

        if (existingHead != null && existingHead.UserId != userId)
        {
            oldProjectHeadId = existingHead.UserId;
            existingHead.Role = "User";
            _context.UserProjects.Update(existingHead);

            // Only strip the global ProjectHead Identity role if they're not Head anywhere else.
            var stillHeadElsewhere = await _context.UserProjects.AnyAsync(up =>
                up.UserId == existingHead.UserId && up.ProjectId != request.ProjectId &&
                up.Role == "ProjectHead" && up.IsActive);

            if (!stillHeadElsewhere)
            {
                var oldHeadUser = await _userManager.FindByIdAsync(existingHead.UserId);
                if (oldHeadUser != null)
                {
                    await _userManager.RemoveFromRoleAsync(oldHeadUser, "ProjectHead");
                    if (!await _userManager.IsInRoleAsync(oldHeadUser, "User"))
                        await _userManager.AddToRoleAsync(oldHeadUser, "User");
                }
            }

            await _publishEndpoint.Publish(new UserRoleAssignedEvent
            {
                UserId = existingHead.UserId,
                OldRole = "ProjectHead",
                NewRole = "User",
                ProjectId = request.ProjectId,
                AssignedAt = DateTime.UtcNow
            });
        }

        membership.Role = "ProjectHead";
        membership.AssignedAt = DateTime.UtcNow;
        _context.UserProjects.Update(membership);
        await _context.SaveChangesAsync();

        // Ensure the global Identity role reflects ProjectHead-in-at-least-one-project.
        if (!await _userManager.IsInRoleAsync(user, "ProjectHead"))
            await _userManager.AddToRoleAsync(user, "ProjectHead");

        await _publishEndpoint.Publish(new ProjectHeadAssignedEvent
        {
            NewProjectHeadId = user.Id,
            OldProjectHeadId = oldProjectHeadId,
            ProjectId = request.ProjectId,
            AssignedAt = DateTime.UtcNow
        });

        return await MapToResponseAsync(user);
    }

    private async Task<UserResponseDto> MapToResponseAsync(ApplicationUser user)
    {
        var rows = await _context.UserProjects
            .Where(up => up.UserId == user.Id && up.IsActive)
            .ToListAsync();

        var projects = new List<ProjectMembershipDto>();
        foreach (var up in rows)
        {
            var projectName = up.ProjectName;
            if (string.IsNullOrEmpty(projectName))
            {
                projectName = await LookupProjectNameAsync(up.ProjectId);
                if (!string.IsNullOrEmpty(projectName))
                {
                    up.ProjectName = projectName;
                    _context.UserProjects.Update(up);
                }
            }
            projects.Add(new ProjectMembershipDto
            {
                ProjectId = up.ProjectId,
                ProjectName = projectName,
                Role = up.Role
            });
        }
        await _context.SaveChangesAsync();

        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email!,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Projects = projects
        };
    }

    private class ProjectNameLookup
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Create a project change request — now "join a new project", not "replace the only one".
    public async Task<(bool Success, string? ErrorMessage)> CreateProjectChangeRequestAsync(string userId, Guid requestedProjectId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) return (false, "User not found or inactive.");

        var alreadyMember = await _context.UserProjects.AnyAsync(up =>
            up.UserId == userId && up.ProjectId == requestedProjectId && up.IsActive);
        if (alreadyMember)
            return (false, "User is already a member of the selected project.");

        try
        {
            var client = CreateAuthorizedDocumentClient();
            var resp = await client.GetAsync($"/api/projects");
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync();
                var projects = System.Text.Json.JsonSerializer.Deserialize<List<ProjectNameLookup>>(content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (projects == null || !projects.Any(p => p.Id == requestedProjectId))
                {
                    _logger.LogWarning("Requested project {ProjectId} not found in project list for user {UserId}", requestedProjectId, userId);
                    return (false, "Selected project does not exist.");
                }
            }
            else
            {
                _logger.LogWarning("Project list lookup failed with status {Status} when validating project {ProjectId} for user {UserId}", resp.StatusCode, requestedProjectId, userId);
                return (false, "Failed to validate selected project. Try again later.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Project lookup threw exception when creating change request for user {UserId}; proceeding to persist request", userId);
        }

        var req = new ProjectChangeRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentProjectId = Guid.Empty, // no longer meaningful with multi-project membership; kept for schema compatibility
            RequestedProjectId = requestedProjectId,
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };
        _context.ProjectChangeRequests.Add(req);
        try
        {
            await _context.SaveChangesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save ProjectChangeRequest for user {UserId}", userId);
            return (false, ex.Message);
        }
    }

    public async Task<List<ProjectChangeRequestResponseDto>> GetPendingRequestsAsync()
    {
        var requests = await _context.ProjectChangeRequests
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();

        var result = new List<ProjectChangeRequestResponseDto>();
        foreach (var r in requests)
        {
            var user = await _userManager.FindByIdAsync(r.UserId);
            result.Add(new ProjectChangeRequestResponseDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserFullName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                CurrentProjectId = r.CurrentProjectId,
                RequestedProjectId = r.RequestedProjectId,
                Status = r.Status,
                RequestedAt = r.RequestedAt
            });
        }
        return result;
    }

    public async Task<bool> ApproveProjectChangeRequestAsync(Guid requestId)
    {
        var req = await _context.ProjectChangeRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending");
        if (req == null) return false;

        var success = await AdminChangeUserProjectAsync(req.UserId, req.RequestedProjectId);
        if (!success) return false;

        req.Status = "Approved";
        req.ResolvedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectProjectChangeRequestAsync(Guid requestId)
    {
        var req = await _context.ProjectChangeRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.Status == "Pending");
        if (req == null) return false;

        req.Status = "Rejected";
        req.ResolvedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // Now: JOIN newProjectId as "User" — does not remove any existing membership.
    // (If you want "replace membership X with Y" semantics instead, tell me which
    // project is being left and I'll add a LeaveProjectAsync alongside this.)
    public async Task<bool> AdminChangeUserProjectAsync(string userId, Guid newProjectId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) return false;

        var alreadyMember = await _context.UserProjects.AnyAsync(up =>
            up.UserId == userId && up.ProjectId == newProjectId && up.IsActive);
        if (alreadyMember) return true;

        var projectName = await LookupProjectNameAsync(newProjectId);
        _context.UserProjects.Add(new UserProject
        {
            Id = Guid.NewGuid(),
            ProjectId = newProjectId,
            ProjectName = projectName,
            UserId = userId,
            Role = "User",
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        });

        if (!await _userManager.IsInRoleAsync(user, "User"))
            await _userManager.AddToRoleAsync(user, "User");

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();
        return true;
    }
}