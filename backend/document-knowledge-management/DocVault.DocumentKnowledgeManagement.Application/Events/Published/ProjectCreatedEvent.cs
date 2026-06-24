namespace DocVault.DocumentKnowledgeManagement.Application.Events.Published;

public class ProjectCreatedEvent
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}