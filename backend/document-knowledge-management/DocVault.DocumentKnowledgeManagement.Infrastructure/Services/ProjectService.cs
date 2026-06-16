using DocVault.DocumentKnowledgeManagement.Application.DTOs.Projects;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Domain.Entities;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    //  Create Project
    public async Task<ProjectResponseDto> CreateProjectAsync(
        CreateProjectDto request, string createdBy)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return MapToResponse(project);
    }

    //  Get All Projects
    public async Task<List<ProjectResponseDto>> GetAllProjectsAsync()
    {
        var projects = await _context.Projects
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToResponse).ToList();
    }

    //  Get Project By Id
    public async Task<ProjectResponseDto?> GetProjectByIdAsync(
        Guid projectId, string userId, string role)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);

        if (project == null)
            return null;

        // Admin can access any project
        if (role == "Admin")
            return MapToResponse(project);

        // ProjectHead and User can only access their own project
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.ProjectId != projectId)
            return null;

        return MapToResponse(project);
    }

    //  Update Project
    public async Task<ProjectResponseDto?> UpdateProjectAsync(
        Guid projectId, UpdateProjectDto request)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);

        if (project == null)
            return null;

        project.Name = request.Name;
        project.Description = request.Description;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(project);
    }

    
    private static ProjectResponseDto MapToResponse(Project project)
    {
        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedBy = project.CreatedBy,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            IsActive = project.IsActive
        };
    }
}