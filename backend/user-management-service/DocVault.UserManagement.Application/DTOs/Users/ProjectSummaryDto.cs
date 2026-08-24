using System;

namespace DocVault.UserManagement.Application.DTOs.Users;

public class ProjectSummaryDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int DocumentCount { get; set; }
}

// this will be used inside the userborad page ....