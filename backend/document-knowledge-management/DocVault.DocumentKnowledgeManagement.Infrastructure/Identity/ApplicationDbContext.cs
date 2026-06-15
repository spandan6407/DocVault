using DocVault.DocumentKnowledgeManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Project → Documents
        builder.Entity<Project>()
            .HasMany(p => p.Documents)
            .WithOne(d => d.Project)
            .HasForeignKey(d => d.ProjectId);

        // Project → Users
        builder.Entity<ApplicationUser>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(u => u.ProjectId)
            .IsRequired(false);

        // Project Column Configs
        builder.Entity<Project>()
            .Property(p => p.Name)
            .HasMaxLength(200);

        builder.Entity<Project>()
            .Property(p => p.Description)
            .HasColumnType("text");

        // Document Column Configs
        builder.Entity<Document>()
            .Property(d => d.Title)
            .HasMaxLength(200);

        builder.Entity<Document>()
            .Property(d => d.Description)
            .HasColumnType("text");

        builder.Entity<Document>()
            .Property(d => d.FileName)
            .HasMaxLength(255);

        builder.Entity<Document>()
            .Property(d => d.FilePath)
            .HasMaxLength(500);

        builder.Entity<Document>()
            .Property(d => d.ContentType)
            .HasMaxLength(100);
    }
}