using DocVault.DocumentKnowledgeManagement.Application.DTOs.Projects;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DocVault.DocumentKnowledgeManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var createdBy = User.FindFirstValue(ClaimTypes.Email)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
                     ?? string.Empty;

        var result = await _projectService.CreateProjectAsync(request, createdBy);

        return CreatedAtAction(nameof(GetProjectById), new { id = result.Id }, result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> GetAllProjects()
    {
        var result = await _projectService.GetAllProjectsAsync();
        return Ok(result);
    }

    // GET /api/projects/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> GetProjectById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? string.Empty;

        var isAdmin = User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");

        // Look up this specific project's claim directly — a user can hold a
        // different role per project, so there's no single "role"/"projectId" pair anymore.
        var roleInThisProject = User.FindFirstValue($"project:{id}");
        var isMember = isAdmin || !string.IsNullOrEmpty(roleInThisProject);

        if (!isMember)
            return NotFound(new { message = "Project not found or access denied." });

        var result = await _projectService.GetProjectByIdAsync(id, userId, roleInThisProject ?? "Admin", id);

        if (result == null)
            return NotFound(new { message = "Project not found or access denied." });

        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _projectService.UpdateProjectAsync(id, request);

        if (result == null)
            return NotFound(new { message = "Project not found." });

        return Ok(result);
    }
}