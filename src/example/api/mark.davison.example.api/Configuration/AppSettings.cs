namespace mark.davison.example.api.Configuration;

public class AppSettings : IRootAppSettings
{
    public string SECTION => "EXAMPLE";

    public bool PRODUCTION_MODE { get; set; }
    public AuthenticationSettings AUTHENTICATION { get; set; } = new();
    public RedisSettings REDIS { get; set; } = new();
    public DatabaseAppSettings DATABASE { get; set; } = new();
}