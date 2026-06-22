namespace DocVault.UserManagement.Domain.Entities;

public class UserProject
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; }
}