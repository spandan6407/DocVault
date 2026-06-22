using Microsoft.AspNetCore.Identity;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    // commment to be removed a
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; } 
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}