using mark.davison.common.server.abstractions.Configuration;

namespace mark.davison.common.authentication.server.Configuration;

public sealed class AuthenticationSettings : IAppSettings
{
    public string? ADMIN_EMAIL { get; set; }
    public List<AuthenticationProviderConfiguration> Providers { get; set; } = [];
}
