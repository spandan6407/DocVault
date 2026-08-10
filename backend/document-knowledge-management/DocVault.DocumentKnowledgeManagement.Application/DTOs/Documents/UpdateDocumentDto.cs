using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;

public class UpdateDocumentDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Optional replacement file
    public IFormFile? File { get; set; }
}
