using System.ComponentModel.DataAnnotations;

namespace DocVault.DocumentKnowledgeManagement.Application.DTOs.Projects;

public class UpdateProjectDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}