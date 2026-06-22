namespace DocVault.UserManagement.Application.Events.Published;

public class UserRoleAssignedEvent
{
    public string UserId { get; set; } = string.Empty;
    public string OldRole { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public DateTime AssignedAt { get; set; }
}