using System.ComponentModel.DataAnnotations;

namespace DocVault.DocumentKnowledgeManagement.Application.DTOs.Users;

public class CreateUserDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}