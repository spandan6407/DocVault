namespace DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;

public class CreateTextDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // typed body text
    // this is used to make the things inside make the note 
    public Guid ProjectId { get; set; }
}
