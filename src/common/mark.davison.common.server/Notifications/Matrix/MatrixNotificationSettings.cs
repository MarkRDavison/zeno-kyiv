namespace mark.davison.common.server.Notifications.Matrix;

public class MatrixNotificationSettings : NotificationSettings
{
    public override string SECTION => "MATRIX";
    public override bool ENABLED { get; set; }
    public string URL { get; set; } = string.Empty;
    public string USERNAME { get; set; } = string.Empty;
    [AppSettingSecret]
    public string PASSWORD { get; set; } = string.Empty;
    public string ROOMID { get; set; } = string.Empty;
    public string SESSIONNAME { get; set; } = string.Empty;
}