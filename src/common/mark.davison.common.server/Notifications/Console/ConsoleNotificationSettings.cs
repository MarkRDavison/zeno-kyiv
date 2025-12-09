namespace mark.davison.common.server.Notifications.Console;

public class ConsoleNotificationSettings : NotificationSettings
{
    public override string SECTION => "CONSOLE";
    public override bool ENABLED { get; set; }
    public LogLevel LOGLEVEL { get; set; } = LogLevel.Information;
}
