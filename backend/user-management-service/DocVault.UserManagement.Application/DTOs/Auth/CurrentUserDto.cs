namespace DocVault.UserManagement.Application.DTOs.Auth;

public class CurrentUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<DocVault.UserManagement.Application.DTOs.Users.ProjectMembershipDto> Projects { get; set; } = new();
    public bool IsActive { get; set; }
}