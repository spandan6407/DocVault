using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;

public class UploadDocumentDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}