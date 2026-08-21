namespace DocVault.UserManagement.Application.Events.Published;

public class UserNotificationEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
