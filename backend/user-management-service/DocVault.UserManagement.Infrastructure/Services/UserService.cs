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
using Microsoft.AspNetCore.Http;

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

    private HttpClient CreateAuthorizedDocumentClient()
    {
        var client = _httpClientFactory.CreateClient("DocumentService");
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        return client;
    }

    // Create User
    public async Task<UserResponseDto?> CreateUserAsync(CreateUserDto request, bool bypassProjectCheck = false)
    {
        var projectExists = true;
        if (!bypassProjectCheck)
        {
            projectExists = await _context.UserProjects
                .AnyAsync(up => up.ProjectId == request.ProjectId && up.IsActive);

            if (!projectExists)
                return null;
        }

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
        {
            try
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Code + ":" + e.Description));
                _logger.LogWarning("User creation failed for {Email}: {Errors}", request.Email, errors);
            }
            catch { }
            return null;
        }

        await _userManager.AddToRoleAsync(user, "User");

        try
        {
            var projectName = string.Empty;
            if (request.ProjectId != Guid.Empty)
            {
                try
                {
                    var client = CreateAuthorizedDocumentClient();
                    var response = await client.GetAsync($"/api/projects/{request.ProjectId}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var proj = System.Text.Json.JsonSerializer.Deserialize<ProjectNameLookup>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        projectName = proj?.Name ?? string.Empty;
                    }
                    else
                    {
                        _logger.LogWarning("Project lookup failed: {Status}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Project lookup threw exception");
                }
            }

            var userProject = new UserProject
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                ProjectName = projectName,
                UserId = user.Id,
                Role = "User",
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserProjects.Add(userProject);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist UserProject for user {UserId}", user.Id);
        }

        await _publishEndpoint.Publish(new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = "User",
            ProjectId = request.ProjectId,
            CreatedAt = user.CreatedAt
        });

        return await MapToResponseAsync(user);
    }

    public async Task<bool> DeleteUserAsync(string userIdToDelete, string requesterId, string requesterRole, Guid? requesterProjectId)
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

            if (requesterProjectId == null || user.ProjectId != requesterProjectId)
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

    public async Task<UserResponseDto?> AssignProjectHeadAsync(
        string userId,
        AssignProjectHeadDto request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return null;

        if (user.ProjectId != request.ProjectId)
            return null;

        var existingHeads = await _userManager.GetUsersInRoleAsync("ProjectHead");
        var existingHead = existingHeads.FirstOrDefault(u => u.ProjectId == request.ProjectId);

        string? oldProjectHeadId = null;

        if (existingHead != null)
        {
            oldProjectHeadId = existingHead.Id;
            await _userManager.RemoveFromRoleAsync(existingHead, "ProjectHead");
            await _userManager.AddToRoleAsync(existingHead, "User");

            await _publishEndpoint.Publish(new UserRoleAssignedEvent
            {
                UserId = existingHead.Id,
                OldRole = "ProjectHead",
                NewRole = "User",
                ProjectId = request.ProjectId,
                AssignedAt = DateTime.UtcNow
            });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, "ProjectHead");

        try
        {
            var up = await _context.UserProjects
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.ProjectId == request.ProjectId);

            var projectName = string.Empty;
            try
            {
                var client = CreateAuthorizedDocumentClient();
                var response = await client.GetAsync($"/api/projects/{request.ProjectId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var proj = System.Text.Json.JsonSerializer.Deserialize<ProjectNameLookup>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    projectName = proj?.Name ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Project lookup threw exception");
            }

            if (up == null)
            {
                up = new UserProject
                {
                    Id = Guid.NewGuid(),
                    ProjectId = request.ProjectId,
                    ProjectName = projectName,
                    UserId = user.Id,
                    Role = "ProjectHead",
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.UserProjects.Add(up);
            }
            else
            {
                up.Role = "ProjectHead";
                up.AssignedAt = DateTime.UtcNow;
                up.IsActive = true;
                if (!string.IsNullOrEmpty(projectName))
                    up.ProjectName = projectName;
                _context.UserProjects.Update(up);
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure UserProject record when assigning project head for user {UserId}", user.Id);
        }

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
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? string.Empty;

        var projectName = string.Empty;
        if (user.ProjectId.HasValue)
        {
            var up = await _context.UserProjects.FirstOrDefaultAsync(u => u.ProjectId == user.ProjectId && u.IsActive);
            projectName = up?.ProjectName ?? string.Empty;

            if (string.IsNullOrEmpty(projectName))
            {
                try
                {
                    var client = CreateAuthorizedDocumentClient();
                    var response = await client.GetAsync($"/api/projects/{user.ProjectId.Value}");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var proj = System.Text.Json.JsonSerializer.Deserialize<ProjectNameLookup>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        projectName = proj?.Name ?? string.Empty;

                        if (!string.IsNullOrEmpty(projectName) && up != null)
                        {
                            up.ProjectName = projectName;
                            _context.UserProjects.Update(up);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Project lookup threw exception in MapToResponseAsync");
                }
            }
        }

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
            CreatedAt = user.CreatedAt,
            ProjectName = projectName
        };
    }

    private class ProjectNameLookup
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Create a project change request (User/ProjectHead)
    public async Task<bool> CreateProjectChangeRequestAsync(string userId, Guid requestedProjectId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) return false;

        // Do not allow requests to the same project
        if (user.ProjectId.HasValue && user.ProjectId.Value == requestedProjectId)
            return false;

        // Optional: verify target project exists by calling Document service. If the lookup fails due to
        // transient errors, allow the request to proceed (we don't want to block users if the doc service
        // is temporarily unavailable). Only reject if the project is confirmed missing (404).
        try
        {
            var client = CreateAuthorizedDocumentClient();
            var resp = await client.GetAsync($"/api/projects/{requestedProjectId}");
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Requested project {ProjectId} not found when creating change request for user {UserId}", requestedProjectId, userId);
                    return false;
                }
                else
                {
                    _logger.LogWarning("Project lookup returned {Status} when creating change request for user {UserId}, proceeding anyway", resp.StatusCode, userId);
                }
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
            CurrentProjectId = user.ProjectId ?? Guid.Empty,
            RequestedProjectId = requestedProjectId,
            Status = "Pending",
            RequestedAt = DateTime.UtcNow
        };
        _context.ProjectChangeRequests.Add(req);
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save ProjectChangeRequest for user {UserId}", userId);
            return false;
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

    public async Task<bool> AdminChangeUserProjectAsync(string userId, Guid newProjectId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive) return false;

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("ProjectHead"))
        {
            // Demote to User in new project
            await _userManager.RemoveFromRoleAsync(user, "ProjectHead");
            await _userManager.AddToRoleAsync(user, "User");
        }

        user.ProjectId = newProjectId;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        return true;
    }
}