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

    // POST /api/projects
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var createdBy = User.FindFirstValue(ClaimTypes.Email)
                     ?? string.Empty;

        var result = await _projectService.CreateProjectAsync(
            request, createdBy);

        return CreatedAtAction(
            nameof(GetProjectById),
            new { id = result.Id },
            result);
    }

    // GET /api/projects
    [HttpGet]
    [Authorize(Roles = "Admin")]
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

        var role = User.FindFirstValue(ClaimTypes.Role)
                ?? string.Empty;

        // Read projectId directly from JWT claim
        var projectIdClaim = User.FindFirstValue("projectId");
        Guid? userProjectId = string.IsNullOrEmpty(projectIdClaim)
            ? null
            : Guid.Parse(projectIdClaim);

        var result = await _projectService
            .GetProjectByIdAsync(id, userId, role, userProjectId);

        if (result == null)
            return NotFound(new { message = "Project not found or access denied." });

        return Ok(result);
    }

    // PUT /api/projects/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProject(
        Guid id, [FromBody] UpdateProjectDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _projectService.UpdateProjectAsync(id, request);

        if (result == null)
            return NotFound(new { message = "Project not found." });

        return Ok(result);
    }
}