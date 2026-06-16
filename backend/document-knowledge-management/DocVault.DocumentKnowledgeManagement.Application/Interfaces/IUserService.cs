using DocVault.DocumentKnowledgeManagement.Application.DTOs.Users;

namespace DocVault.DocumentKnowledgeManagement.Application.Interfaces;

public interface IUserService
{
    Task<UserResponseDto?> CreateUserAsync(CreateUserDto request);

    Task<List<UserResponseDto>> GetAllUsersAsync();

    Task<List<UserResponseDto>> GetUsersByProjectAsync(
        Guid projectId, string requesterId, string requesterRole);

    Task<UserResponseDto?> AssignProjectHeadAsync(
        string userId, AssignProjectHeadDto request);
}