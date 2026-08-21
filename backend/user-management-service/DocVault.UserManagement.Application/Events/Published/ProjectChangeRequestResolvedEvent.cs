namespace DocVault.UserManagement.Application.Events.Published;

public class ProjectChangeRequestResolvedEvent
{
    public string UserId { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public string Status { get; set; } = string.Empty; // Approved/Rejected
    public DateTime ResolvedAt { get; set; }
}
