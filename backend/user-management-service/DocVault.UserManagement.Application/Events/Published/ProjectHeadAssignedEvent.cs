namespace DocVault.UserManagement.Application.Events.Published;

public class ProjectHeadAssignedEvent
{
    public string NewProjectHeadId { get; set; } = string.Empty;
    public string? OldProjectHeadId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime AssignedAt { get; set; }
}