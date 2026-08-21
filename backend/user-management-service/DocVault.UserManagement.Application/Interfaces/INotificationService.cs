namespace DocVault.UserManagement.Application.Interfaces;

public interface INotificationService
{
    // Send a notification message (JSON string) to a specific user by id
    Task SendToUserAsync(string userId, string message);

    // Send a notification message to all connected admins
    Task SendToAdminsAsync(string message);

    // Register and return a channel writer for server-sent events streaming for a given connection
    // Implementation exposes a method to create and manage connections via controller
}
