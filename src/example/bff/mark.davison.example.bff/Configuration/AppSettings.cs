namespace mark.davison.example.bff.Configuration;

public sealed class AppSettings : IRootAppSettings
{
    public string SECTION => "EXAMPLE";
    public bool PRODUCTION_MODE { get; set; }
    public AuthenticationSettings AUTHENTICATION { get; set; } = new();
    public RedisSettings REDIS { get; set; } = new();
    public string API_ENDPOINT { get; set; } = "https://localhost:50000";
    public string WEB_ORIGIN { get; set; } = "https://localhost:8080";
}
