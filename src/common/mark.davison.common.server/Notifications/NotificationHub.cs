namespace mark.davison.common.server.Notifications;

public sealed class NotificationHub : INotificationHub
{
    private readonly List<INotificationService> _notificationServices;

    public NotificationHub(IEnumerable<INotificationService> notificationServices)
    {
        _notificationServices = notificationServices.ToList();
    }

    public async Task<Response> SendNotification(NotificationMessage message)
    {
        var response = new Response();

        foreach (var service in _notificationServices)
        {
            if (!service.Settings.ENABLED)
            {
                continue;
            }

            var serviceResponse = await service.SendNotification(message);

            response.Errors.AddRange(serviceResponse.Errors);
            response.Warnings.AddRange(serviceResponse.Warnings);
        }

        return response;
    }
}
