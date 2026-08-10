using MassTransit;

namespace DocVault.Shared.Contracts.Events;

[EntityName("project-created")]
public class ProjectCreatedEvent
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}