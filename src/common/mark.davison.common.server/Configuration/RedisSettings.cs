namespace mark.davison.common.server.Configuration;

public class RedisSettings : IAppSettings
{
    public string INSTANCE_NAME { get; set; } = string.Empty;
    public string HOST { get; set; } = string.Empty;
    public int PORT { get; set; } = 6379;
    [AppSettingSecret]
    public string? PASSWORD { get; set; }
}
