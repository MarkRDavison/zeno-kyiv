using mark.davison.common.server.abstractions.Configuration;

namespace mark.davison.kyiv.api.Configuration;

public class AppSettings : IRootAppSettings
{
    public string SECTION => "KYIV";

    public bool PRODUCTION_MODE { get; set; }
    public AuthenticationSettings AUTHENTICATION { get; set; } = new();
    public RedisSettings REDIS { get; set; } = new();
}
