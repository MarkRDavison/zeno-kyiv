namespace mark.davison.common.server.abstractions.Notifications;

public abstract class NotificationSettings : IAppSettings
{
    public abstract string SECTION { get; }
    public abstract bool ENABLED { get; set; }
}
