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

    [HttpGet("user-projects")]
    [Authorize]
    public async Task<IActionResult> GetUserProjects()
    {
        if (!await IsAdminAsync(User))
            return Forbid();
        var result = await _userService.GetAllUserProjectsAsync();
        return Ok(result);
    }

    [HttpGet("projects/{projectId}/users")]
    [Authorize(Roles = "Admin,ProjectHead")]
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
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? string.Empty;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var appUser = await _userManager.FindByIdAsync(userId);
        if (appUser == null)
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(appUser);
        if (!roles.Contains("User") && !roles.Contains("ProjectHead") && !roles.Contains("Admin"))
            return Forbid();

        var result = await _userService.CreateProjectChangeRequestAsync(userId, request.RequestedProjectId);
        if (!result) return BadRequest(new { message = "Request failed." });
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