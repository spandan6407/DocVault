using DocVault.UserManagement.Application.Events.Published;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DocVault.UserManagement.Infrastructure.Messaging.Consumers;

public class UserNotificationConsumer : IConsumer<UserNotificationEvent>
{
    private readonly ILogger<UserNotificationConsumer> _logger;
    private readonly DocVault.UserManagement.Infrastructure.Services.NotificationBroker _broker;

    public UserNotificationConsumer(ILogger<UserNotificationConsumer> logger, DocVault.UserManagement.Infrastructure.Services.NotificationBroker broker)
    {
        _logger = logger;
        _broker = broker;
    }

    public Task Consume(ConsumeContext<UserNotificationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("UserNotification received for {UserId}: {Message}", msg.UserId, msg.Message);
        // Push into SSE broker
        try
        {
            _broker.PublishToUser(msg.UserId, msg.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push SSE notification to user {UserId}", msg.UserId);
        }
        return Task.CompletedTask;
    }
}
