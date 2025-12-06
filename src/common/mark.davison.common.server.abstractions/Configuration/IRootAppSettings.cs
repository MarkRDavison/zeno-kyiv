namespace mark.davison.common.server.abstractions.Configuration;

public interface IRootAppSettings : IAppSettings
{
    bool PRODUCTION_MODE { get; set; }
}
