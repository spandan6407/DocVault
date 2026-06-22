using DocVault.UserManagement.Application.Events.Consumed;
using DocVault.UserManagement.Domain.Entities;
using DocVault.UserManagement.Infrastructure.Identity;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocVault.UserManagement.Infrastructure.Messaging.Consumers;

public class ProjectCreatedConsumer : IConsumer<ProjectCreatedEvent>
{
    private readonly UserManagementDbContext _context;
    private readonly ILogger<ProjectCreatedConsumer> _logger;

    public ProjectCreatedConsumer(
        UserManagementDbContext context,
        ILogger<ProjectCreatedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProjectCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "✅ Project Created Event Received: {ProjectId} - {ProjectName}",
            message.ProjectId,
            message.ProjectName);

        // ✅ Check if project already exists
        var exists = await _context.UserProjects
            .AnyAsync(up => up.ProjectId == message.ProjectId);

        if (exists)
        {
            _logger.LogInformation(
                "Project already exists: {ProjectId}", message.ProjectId);
            return;
        }

        //  Save project reference
        var userProject = new UserProject
        {
            Id = Guid.NewGuid(),
            ProjectId = message.ProjectId,
            ProjectName = message.ProjectName,
            UserId = message.CreatedBy,
            Role = "Admin",
            AssignedAt = message.CreatedAt,
            IsActive = true
        };

        _context.UserProjects.Add(userProject);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "✅ Project saved to UserDB: {ProjectId}", message.ProjectId);
    }
}