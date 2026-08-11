namespace DocVault.UserManagement.Application.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public List<DocVault.UserManagement.Application.DTOs.Users.ProjectMembershipDto> Projects { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}