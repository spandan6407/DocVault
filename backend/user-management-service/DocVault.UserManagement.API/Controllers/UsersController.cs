using DocVault.UserManagement.Application.DTOs.Users;
using DocVault.UserManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace DocVault.UserManagement.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly Microsoft.AspNetCore.Identity.UserManager<DocVault.UserManagement.Infrastructure.Identity.ApplicationUser> _userManager;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger,
        Microsoft.AspNetCore.Identity.UserManager<DocVault.UserManagement.Infrastructure.Identity.ApplicationUser> userManager)
    {
        _userService = userService;
        _logger = logger;
        _userManager = userManager;
    }

    private async Task<bool> IsAdminAsync(System.Security.Claims.ClaimsPrincipal user)
    {
        if (user == null) return false;

        if (user.HasClaim(c => (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && c.Value == "Admin"))
            return true;

        var sub = user.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(sub))
            return false;

        try
        {
            var appUser = await _userManager.FindByIdAsync(sub);
            if (appUser == null)
                return false;

            return await _userManager.IsInRoleAsync(appUser, "Admin");
        }
        catch
        {
            return false;
        }
    }

    // Every "project:{guid}" claim on the token, keyed by project id, valued by that project's role.
    private static Dictionary<Guid, string> GetProjectRoleClaims(ClaimsPrincipal user)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var claim in user.Claims)
        {
            if (claim.Type.StartsWith("project:") &&
                Guid.TryParse(claim.Type.Substring("project:".Length), out var projectId))
            {
                map[projectId] = claim.Value;
            }
        }
        return map;
    }

    [HttpPost("users")]
    [Authorize]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto request)
    {
        if (!await IsAdminAsync(User))
        {
            _logger?.LogWarning("CreateUser forbidden: caller is not admin.");
            return Forbid();
        }
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(request, bypassProjectCheck: true);
        if (result == null)
            return BadRequest(new { message = "User creation failed." });

        return Ok(result);
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin,ProjectHead")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                         ?? string.Empty;
        var requesterRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        // A ProjectHead may now be head of several projects — collect all of them
        // so the service can check "is the target a member of any project I head".
        var headProjectIds = GetProjectRoleClaims(User)
            .Where(kv => kv.Value == "ProjectHead")
            .Select(kv => kv.Key)
            .ToList();

        var result = await _userService.DeleteUserAsync(id, requesterId, requesterRole, headProjectIds);
        if (!result)
            return BadRequest(new { message = "Delete failed. Check permissions." });

        return Ok(new { message = "User deleted successfully." });
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetAllUsers()
    {
        if (!await IsAdminAsync(User))
            return Forbid();
        var result = await _userService.GetAllUsersAsync();
        return Ok(result);
    }

    [HttpGet("users/search")]
    [Authorize]
    public async Task<IActionResult> SearchUsers([FromQuery] string? query, [FromQuery] Guid? projectId, [FromQuery] string? role)
    {
        if (!await IsAdminAsync(User))
            return Forbid();
        var result = await _userService.SearchUsersAsync(query, projectId, role);
        return Ok(result);
    }

    [HttpGet("user-projects")]
    [Authorize]
    public async Task<IActionResult> GetUserProjects()
    {
        if (!await IsAdminAsync(User))
            return Forbid();
        var result = await _userService.GetAllUserProjectsAsync();
        return Ok(result);
    }

    // GET /api/users/{id}/projects
    [HttpGet("users/{id}/projects")]
    [Authorize]
    public async Task<IActionResult> GetProjectsForUser(string id)
    {
        if (!await IsAdminAsync(User)) return Forbid();
        var result = await _userService.GetUserProjectsAsync(id);
        return Ok(result);
    }

    // POST /api/users/{id}/projects
    [HttpPost("users/{id}/projects")]
    [Authorize]
    public async Task<IActionResult> AddUserProject(string id, [FromBody] DocVault.UserManagement.Application.DTOs.Users.AddUserProjectDto request)
    {
        if (!await IsAdminAsync(User)) return Forbid();
        var result = await _userService.AddUserToProjectAsync(id, request);
        if (result == null) return BadRequest(new { message = "Add membership failed or already exists." });
        return Ok(result);
    }

    // PUT /api/users/{id}/projects/{projectId}/role
    [HttpPut("users/{id}/projects/{projectId}/role")]
    [Authorize]
    public async Task<IActionResult> UpdateUserProjectRole(string id, Guid projectId, [FromBody] ChangeUserProjectRoleDto request)
    {
        if (!await IsAdminAsync(User)) return Forbid();
        var ok = await _userService.UpdateUserProjectRoleAsync(id, projectId, request);
        if (!ok) return BadRequest(new { message = "Update role failed or membership not found." });
        return Ok(new { message = "Role updated." });
    }

    // DELETE /api/users/{id}/projects/{projectId}
    [HttpDelete("users/{id}/projects/{projectId}")]
    [Authorize]
    public async Task<IActionResult> RemoveUserProject(string id, Guid projectId)
    {
        if (!await IsAdminAsync(User)) return Forbid();
        var ok = await _userService.RemoveUserFromProjectAsync(id, projectId);
        if (!ok) return BadRequest(new { message = "Remove membership failed or not found." });
        return Ok(new { message = "Membership removed." });
    }

    [HttpGet("projects/{projectId}/users")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> GetUsersByProject(Guid projectId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var requesterRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        var result = await _userService.GetUsersByProjectAsync(projectId, requesterId, requesterRole);
        return Ok(result);
    }

    [HttpPut("users/{id}/assign-project-head")]
    [Authorize]
    public async Task<IActionResult> AssignProjectHead(string id, [FromBody] AssignProjectHeadDto request)
    {
        if (!await IsAdminAsync(User))
            return Forbid();
        var result = await _userService.AssignProjectHeadAsync(id, request);

        if (result == null)
            return BadRequest(new { message = "Assignment failed." });

        return Ok(result);
    }

    [HttpPost("users/project-change-request")]
    [Authorize]
    public async Task<IActionResult> RequestProjectChange([FromBody] CreateProjectChangeRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            // Collect model state errors into a simpler array to return to the caller
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            _logger?.LogWarning("Invalid project change request body: {Errors}", string.Join("; ", errors));
            return BadRequest(new { message = "Invalid request body.", errors });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (request == null || request.RequestedProjectId == Guid.Empty)
        {
            _logger?.LogWarning("RequestedProjectId missing or invalid for user {UserId}", userId);
            return BadRequest(new { message = "RequestedProjectId is required and must be a valid GUID." });
        }

        // use _userManager directly (no change)
        var appUser = await _userManager.FindByIdAsync(userId);
        if (appUser == null)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(appUser);
        if (!roles.Contains("User") && !roles.Contains("ProjectHead") && !roles.Contains("Admin"))
            return Forbid();

        // Pre-validate: user is not already a member of the requested project
        var currentMemberships = await _userService.GetUserProjectsAsync(userId);
        if (currentMemberships.Any(up => up.ProjectId == request.RequestedProjectId && up.IsActive))
        {
            return BadRequest(new { message = "User is already a member of the selected project." });
        }

        // Pre-validate: no existing pending request for same user + project
        var pending = await _userService.GetPendingRequestsAsync();
        if (pending.Any(r => r.UserId == userId && r.RequestedProjectId == request.RequestedProjectId && r.Status == "Pending"))
        {
            return BadRequest(new { message = "A pending request for this project already exists." });
        }

        var (success, errorMessage) = await _userService.CreateProjectChangeRequestAsync(userId, request.RequestedProjectId);
        if (!success)
            return BadRequest(new { message = errorMessage ?? "Request failed. Project may not exist or an error occurred." });

        return Ok(new { message = "Request submitted." });
    }

    [HttpGet("project-change-requests")]
    [Authorize]
    public async Task<IActionResult> GetPendingRequests()
    {
        if (!await IsAdminAsync(User)) return Forbid();
        var result = await _userService.GetPendingRequestsAsync();
        return Ok(result);
    }

    [HttpPut("project-change-requests/{id}/approve")]
    [Authorize]
    public async Task<IActionResult> ApproveRequest(Guid id)
    {
        if (!await IsAdminAsync(User)) return Forbid();
        var result = await _userService.ApproveProjectChangeRequestAsync(id);
        if (!result) return BadRequest(new { message = "Approve failed." });
        return Ok(new { message = "Approved." });
    }

    [HttpPut("project-change-requests/{id}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectRequest(Guid id)
    {
        if (!await IsAdminAsync(User)) return Forbid();
        var result = await _userService.RejectProjectChangeRequestAsync(id);
        if (!result) return BadRequest(new { message = "Reject failed." });
        return Ok(new { message = "Rejected." });
    }

    [HttpPut("users/{id}/change-project")]
    [Authorize]
    public async Task<IActionResult> ChangeUserProject(string id, [FromBody] CreateProjectChangeRequestDto request)
    {
        if (!await IsAdminAsync(User)) return Forbid();
        var result = await _userService.AdminChangeUserProjectAsync(id, request.RequestedProjectId);
        if (!result) return BadRequest(new { message = "Change failed." });
        return Ok(new { message = "Project changed." });
    }
}