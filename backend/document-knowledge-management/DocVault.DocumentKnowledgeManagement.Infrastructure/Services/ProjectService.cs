using DocVault.DocumentKnowledgeManagement.Application.DTOs.Projects;
using DocVault.Shared.Contracts.Events;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Domain.Entities;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        ApplicationDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    // Create Project
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

        _logger.LogInformation(
            "Publishing ProjectCreatedEvent for project {ProjectId} - {ProjectName}",
            project.Id, project.Name);

        // Publish Event to RabbitMQ
        await _publishEndpoint.Publish(new ProjectCreatedEvent
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            CreatedBy = createdBy,
            CreatedAt = project.CreatedAt
        });

        _logger.LogInformation(
            "ProjectCreatedEvent published successfully for project {ProjectId}",
            project.Id);

        return MapToResponse(project);
    }

    // Get All Projects
    public async Task<List<ProjectResponseDto>> GetAllProjectsAsync()
    {
        var projects = await _context.Projects
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return projects.Select(MapToResponse).ToList();
    }

    // Get Project By Id
    public async Task<ProjectResponseDto?> GetProjectByIdAsync(
        Guid projectId, string userId, string role, Guid? userProjectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);

        if (project == null)
            return null;

        // Admin can access any project
        if (role == "Admin")
            return MapToResponse(project);

        // ProjectHead and User — validate against JWT claim directly
        if (userProjectId == null || userProjectId != projectId)
            return null;

        return MapToResponse(project);
    }

    // Update Project
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