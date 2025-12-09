namespace mark.davison.common.persistence.Configuration;

[ExcludeFromCodeCoverage]
public class DatabaseAppSettings : IAppSettings
{
    public string SECTION => "DATABASE";
    public DatabaseType DATABASE_TYPE { get; set; }
    [AppSettingSecret]
    public string CONNECTION_STRING { get; set; } = string.Empty;
    public int DB_PORT { get; set; }
    public string DB_HOST { get; set; } = string.Empty;
    public string DB_DATABASE { get; set; } = string.Empty;
    public string DB_USERNAME { get; set; } = string.Empty;
    [AppSettingSecret]
    public string DB_PASSWORD { get; set; } = string.Empty;
}