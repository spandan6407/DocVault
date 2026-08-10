using DocVault.UserManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocVault.UserManagement.Infrastructure.Identity;

public class UserManagementDbContext : IdentityDbContext<ApplicationUser>
{
    public UserManagementDbContext(
        DbContextOptions<UserManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProject> UserProjects { get; set; }
    public DbSet<ProjectChangeRequest> ProjectChangeRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

         //User → Project relationship
        builder.Entity<UserProject>()
            .HasIndex(up => new { up.UserId, up.ProjectId })
            .IsUnique();

        
        builder.Entity<UserProject>()
            .Property(up => up.Role)
            .HasMaxLength(50);

        builder.Entity<UserProject>()
            .Property(up => up.ProjectName)
            .HasMaxLength(200);

        builder.Entity<ProjectChangeRequest>()
            .Property(r => r.Status)
            .HasMaxLength(20);
    }
}