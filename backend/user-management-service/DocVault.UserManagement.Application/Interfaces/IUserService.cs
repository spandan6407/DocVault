using DocVault.UserManagement.Application.DTOs.Users;

namespace DocVault.UserManagement.Application.Interfaces;

public interface IUserService
{
    Task<UserResponseDto?> CreateUserAsync(CreateUserDto request, bool bypassProjectCheck = false);
    Task<List<UserResponseDto>> GetAllUsersAsync();
    Task<List<UserResponseDto>> GetUsersByProjectAsync(
        Guid projectId,
        string requesterId,
        string requesterRole);
    Task<UserResponseDto?> AssignProjectHeadAsync(
        string userId,
        AssignProjectHeadDto request);

    // Diagnostic: list all user-project entries
    Task<List<UserProjectDto>> GetAllUserProjectsAsync();

    // Delete a user (soft-delete). Returns true on success; enforces requester permissions.
    Task<bool> DeleteUserAsync(string userIdToDelete, string requesterId, string requesterRole, Guid? requesterProjectId);

    Task<bool> CreateProjectChangeRequestAsync(string userId, Guid requestedProjectId);
    Task<List<ProjectChangeRequestResponseDto>> GetPendingRequestsAsync();
    Task<bool> ApproveProjectChangeRequestAsync(Guid requestId);
    Task<bool> RejectProjectChangeRequestAsync(Guid requestId);
    Task<bool> AdminChangeUserProjectAsync(string userId, Guid newProjectId);
}