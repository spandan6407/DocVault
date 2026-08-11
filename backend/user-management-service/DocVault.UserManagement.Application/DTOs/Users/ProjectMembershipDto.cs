namespace DocVault.UserManagement.Application.DTOs.Users;

public class ProjectMembershipDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
