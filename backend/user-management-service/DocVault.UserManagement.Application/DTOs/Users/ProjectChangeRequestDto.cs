namespace DocVault.UserManagement.Application.DTOs.Users;

public class CreateProjectChangeRequestDto
{
    public Guid RequestedProjectId { get; set; }
}

public class ProjectChangeRequestResponseDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public Guid CurrentProjectId { get; set; }
    public Guid RequestedProjectId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}


