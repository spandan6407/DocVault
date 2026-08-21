using DocVault.UserManagement.Application.Events.Published;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DocVault.UserManagement.Infrastructure.Messaging.Consumers;

public class AdminNotificationConsumer : IConsumer<AdminNotificationEvent>
{
    private readonly ILogger<AdminNotificationConsumer> _logger;

    public AdminNotificationConsumer(ILogger<AdminNotificationConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<AdminNotificationEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("AdminNotification received: {Message}", msg.Message);
        // For now, we just log. A separate notification service or frontend adapter can consume this exchange.
        return Task.CompletedTask;
    }
}
