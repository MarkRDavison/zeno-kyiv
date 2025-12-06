using mark.davison.common.server.abstractions.Configuration;

namespace mark.davison.kyiv.bff.Configuration;

public class AppSettings : IRootAppSettings
{
    public string SECTION => "KYIV";
    public bool PRODUCTION_MODE { get; set; }
    public AuthenticationSettings AUTHENTICATION { get; set; } = new();
    public RedisSettings REDIS { get; set; } = new();
    public string API_ENDPOINT { get; set; } = "https://localhost:50000/";
}
