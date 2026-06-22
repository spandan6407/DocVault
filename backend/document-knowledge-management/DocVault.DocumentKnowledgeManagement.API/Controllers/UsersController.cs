using DocVault.DocumentKnowledgeManagement.Application.DTOs.Users;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocVault.DocumentKnowledgeManagement.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    //  POST /api/users
    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userService.CreateUserAsync(request);

        if (result == null)
            return BadRequest(new
            {
                message = "User creation failed. Check role and project."
            });

        return Ok(result);
    }

    //  GET /api/users....
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _userService.GetAllUsersAsync();
        return Ok(result);
    }

    //  GET /api/projects/{projectId}/users....
    [HttpGet("projects/{projectId}/users")]
    [Authorize(Roles = "Admin,ProjectHead")]
    public async Task<IActionResult> GetUsersByProject(Guid projectId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? string.Empty;
        var requesterRole = User.FindFirstValue(ClaimTypes.Role)
                         ?? string.Empty;

        var result = await _userService.GetUsersByProjectAsync(
            projectId, requesterId, requesterRole);

        return Ok(result);
    }

    //  PUT /api/users/{id}/assign-project-head
    [HttpPut("users/{id}/assign-project-head")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignProjectHead(
        string id, [FromBody] AssignProjectHeadDto request)
    {
        var result = await _userService.AssignProjectHeadAsync(id, request);

        if (result == null)
            return BadRequest(new
            {
                message = "Assignment failed. Check user and project."
            });

        return Ok(result);
    }
}