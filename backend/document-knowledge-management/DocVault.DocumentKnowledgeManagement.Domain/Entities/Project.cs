using System.Reflection.Metadata;

namespace DocVault.DocumentKnowledgeManagement.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }


    public ICollection<Document> Documents { get; set; }
        = new List<Document>();
}   