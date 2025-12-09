namespace mark.davison.common.server.abstractions.Notifications;

public interface INotificationService
{
    NotificationSettings Settings { get; }
    Task<Response> SendNotification(NotificationMessage message);
}
