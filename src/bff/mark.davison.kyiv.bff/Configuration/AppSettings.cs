namespace mark.davison.kyiv.bff.Configuration;

public class AppSettings
{
    public const string SECTION = "KYIV";
    public bool PRODUCTION_MODE { get; set; }
    public AuthenticationSettings AUTHENTICATION { get; set; } = new();
    public RedisSettings REDIS { get; set; } = new();
    public string API_ENDPOINT { get; set; } = "https://localhost:50000/";
}
