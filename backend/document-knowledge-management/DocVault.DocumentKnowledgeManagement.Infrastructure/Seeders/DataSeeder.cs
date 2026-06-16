using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Seeders;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        
        await SeedRolesAsync(roleManager, logger);

        
        await SeedAdminAsync(userManager, logger);
    }

    //  Seed Roles
    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        string[] roles = { "Admin", "ProjectHead", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Role created: {Role}", role);
            }
            else
            {
                logger.LogInformation("Role already exists: {Role}", role);
            }
        }
    }

    
    private static async Task SeedAdminAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        const string adminEmail = "admin@docvault.com";
        const string adminPassword = "Admin@DocVault#2024";

        
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

        if (existingAdmin != null)
        {
            logger.LogInformation("Admin already exists. Skipping.");
            return;
        }

        
        var adminUser = new ApplicationUser
        {
            FirstName = "System",
            LastName = "Admin",
            Email = adminEmail,
            UserName = adminEmail,
            ProjectId = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            
            await userManager.AddToRoleAsync(adminUser, "Admin");
            logger.LogInformation("Admin created successfully: {Email}", adminEmail);
        }
        else
        {
            foreach (var error in result.Errors)
            {
                logger.LogError("Admin creation error: {Error}", error.Description);
            }
        }
    }
}