using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace DocVault.UserManagement.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly DocVault.UserManagement.Infrastructure.Services.NotificationBroker _broker;

    public NotificationsController(DocVault.UserManagement.Infrastructure.Services.NotificationBroker broker)
    {
        _broker = broker;
    }

    [HttpGet("subscribe/user/{userId}")]
    public async Task SubscribeUser(string userId)
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        var reader = _broker.SubscribeUser(userId);
        try
        {
            await foreach (var msg in reader.ReadAllAsync(HttpContext.RequestAborted))
            {
                await Response.WriteAsync($"data: {msg}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _broker.UnsubscribeUser(userId, reader);
        }
    }

    [HttpGet("subscribe/admins")]
    public async Task SubscribeAdmins()
    {
        Response.Headers.Add("Content-Type", "text/event-stream");
        var reader = _broker.SubscribeAdmin();
        try
        {
            await foreach (var msg in reader.ReadAllAsync(HttpContext.RequestAborted))
            {
                await Response.WriteAsync($"data: {msg}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _broker.UnsubscribeAdmin(reader);
        }
    }
}
