namespace mark.davison.kyiv.api.Configuration;

public class AppSettings
{
    public const string SECTION = "KYIV";

    public bool PRODUCTION_MODE { get; set; }
    public AuthenticationSetings AUTHENTICATION { get; set; } = new();
}
