namespace mark.davison.common.server.abstractions.Configuration;

public interface IRootAppSettings : IAppSettings
{
    string SECTION { get; }
    bool PRODUCTION_MODE { get; set; }
}
