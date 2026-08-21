namespace DocVault.UserManagement.Application.Events.Published;

public class AdminNotificationEvent
{
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
