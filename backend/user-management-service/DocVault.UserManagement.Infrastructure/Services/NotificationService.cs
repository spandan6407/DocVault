using DocVault.UserManagement.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DocVault.UserManagement.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<NotificationService> _logger;
        private readonly DocVault.UserManagement.Infrastructure.Services.NotificationBroker _broker;

    public NotificationService(IPublishEndpoint publishEndpoint, ILogger<NotificationService> logger, DocVault.UserManagement.Infrastructure.Services.NotificationBroker broker)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _broker = broker;
    }

    public async Task SendToUserAsync(string userId, string message)
    {
        // Publish a direct notification event that other services (including frontend adapters) can consume
        await _publishEndpoint.Publish(new DocVault.UserManagement.Application.Events.Published.UserNotificationEvent
        {
            UserId = userId,
            Message = message,
            SentAt = DateTime.UtcNow
        });
        _logger.LogInformation("Published notification for user {UserId}", userId);
        // Also push to in-memory SSE broker for connected clients
        try
        {
            _broker.PublishToUser(userId, message);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to push SSE notification to user {UserId}", userId);
        }
    }

    public async Task SendToAdminsAsync(string message)
    {
        await _publishEndpoint.Publish(new DocVault.UserManagement.Application.Events.Published.AdminNotificationEvent
        {
            Message = message,
            SentAt = DateTime.UtcNow
        });
        _logger.LogInformation("Published admin notification");
        try
        {
            _broker.PublishToAdmins(message);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to push SSE admin notification");
        }
    }
}




