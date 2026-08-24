using System;

namespace DocVault.UserManagement.Application.DTOs.Users;

public class AddUserProjectDto
{
    public Guid ProjectId { get; set; }
    public string Role { get; set; } = string.Empty; // "User" or "ProjectHead"
}
