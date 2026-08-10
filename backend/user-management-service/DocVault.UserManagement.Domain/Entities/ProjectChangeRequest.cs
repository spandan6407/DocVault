namespace DocVault.UserManagement.Domain.Entities;

public class ProjectChangeRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid CurrentProjectId { get; set; }
    public Guid RequestedProjectId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public DateTime RequestedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
