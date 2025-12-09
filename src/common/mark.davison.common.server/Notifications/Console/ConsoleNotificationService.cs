namespace mark.davison.common.server.Notifications.Console;

public sealed class ConsoleNotificationService : IConsoleNotificationService
{
    private readonly ILogger<ConsoleNotificationService> _logger;
    private readonly ConsoleNotificationSettings _settings;
    public ConsoleNotificationService(
        IOptions<ConsoleNotificationSettings> options,
        ILogger<ConsoleNotificationService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public NotificationSettings Settings => _settings;

    public Task<Response> SendNotification(NotificationMessage message)
    {
        _logger.Log(_settings.LOGLEVEL, message.Message);
        return Task.FromResult(new Response());
    }
}
