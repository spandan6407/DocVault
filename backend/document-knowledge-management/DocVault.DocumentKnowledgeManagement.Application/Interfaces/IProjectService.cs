using DocVault.DocumentKnowledgeManagement.Application.DTOs.Projects;

namespace DocVault.DocumentKnowledgeManagement.Application.Interfaces;

public interface IProjectService
{
    Task<ProjectResponseDto> CreateProjectAsync(
        CreateProjectDto request, string createdBy);

    Task<List<ProjectResponseDto>> GetAllProjectsAsync();

    Task<ProjectResponseDto?> GetProjectByIdAsync(
        Guid projectId, string userId, string role);

    Task<ProjectResponseDto?> UpdateProjectAsync(
        Guid projectId, UpdateProjectDto request);
}