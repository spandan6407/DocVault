namespace DocVault.UserManagement.Application.Events.Consumed;

public class ProjectCreatedEvent
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}