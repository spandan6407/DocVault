using DocVault.UserManagement.Application.DTOs.Users;

namespace DocVault.UserManagement.Application.Interfaces;

public interface IUserService
{
    Task<UserResponseDto?> CreateUserAsync(CreateUserDto request, bool bypassProjectCheck = false);
    Task<bool> DeleteUserAsync(string userIdToDelete, string requesterId, string requesterRole, List<Guid> requesterHeadProjectIds);
    Task<List<UserResponseDto>> GetAllUsersAsync();
    Task<List<UserProjectDto>> GetAllUserProjectsAsync();
    Task<List<UserProjectDto>> GetUserProjectsAsync(string userId);
    Task<List<UserResponseDto>> GetUsersByProjectAsync(Guid projectId, string requesterId, string requesterRole);
    Task<UserResponseDto?> AssignProjectHeadAsync(string userId, AssignProjectHeadDto request);
    Task<UserProjectDto?> AddUserToProjectAsync(string userId, AddUserProjectDto request);
    Task<bool> UpdateUserProjectRoleAsync(string userId, Guid projectId, ChangeUserProjectRoleDto request);
    Task<bool> RemoveUserFromProjectAsync(string userId, Guid projectId);
    Task<List<UserResponseDto>> SearchUsersAsync(string? query, Guid? projectId, string? role);
    // Returns (Success, ErrorMessage). ErrorMessage is non-null when Success==false.
    Task<(bool Success, string? ErrorMessage)> CreateProjectChangeRequestAsync(string userId, Guid requestedProjectId);
    Task<List<ProjectChangeRequestResponseDto>> GetPendingRequestsAsync();
    Task<bool> ApproveProjectChangeRequestAsync(Guid requestId);
    Task<bool> RejectProjectChangeRequestAsync(Guid requestId);
    Task<bool> AdminChangeUserProjectAsync(string userId, Guid newProjectId);
}