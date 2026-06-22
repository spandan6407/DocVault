using DocVault.UserManagement.Application.DTOs.Auth;

namespace DocVault.UserManagement.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    Task<CurrentUserDto?> GetCurrentUserAsync(string userId);
}