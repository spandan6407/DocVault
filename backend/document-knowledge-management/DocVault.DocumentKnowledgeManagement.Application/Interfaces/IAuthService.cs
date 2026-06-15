using DocVault.DocumentKnowledgeManagement.Application.DTOs.Auth;

namespace DocVault.DocumentKnowledgeManagement.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    Task<CurrentUserDto?> GetCurrentUserAsync(string userId);
}