namespace mark.davison.common.server.sample.api.Configuration;

public class AppSettings : IRootAppSettings
{
    public string API_ORIGIN { get; set; } = string.Empty;
    public string SECTION => "SAMPLE";
    public bool PRODUCTION_MODE { get; set; }
}