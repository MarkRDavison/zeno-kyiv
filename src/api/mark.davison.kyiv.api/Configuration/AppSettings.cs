namespace mark.davison.kyiv.api.Configuration;

public class AppSettings
{
    public const string SECTION = "KYIV";

    public bool PRODUCTION_MODE { get; set; }
    public string? ADMIN_EMAIL { get; set; }
    public AuthenticationSettings AUTHENTICATION { get; set; } = new();
    public RedisSettings REDIS { get; set; } = new();
}
