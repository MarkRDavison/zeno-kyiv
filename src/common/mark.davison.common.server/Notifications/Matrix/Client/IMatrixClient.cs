namespace mark.davison.common.server.Notifications.Matrix.Client;

public interface IMatrixClient
{
    Task<Response> SendMessage(string roomId, NotificationMessage message);
}