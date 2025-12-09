namespace mark.davison.common.server.abstractions.Notifications;

public interface INotificationHub
{
    Task<Response> SendNotification(NotificationMessage message);
}
