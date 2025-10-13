namespace mark.davison.common.authentication.server.Configuration;

public sealed class AuthenticationSettings
{
    public List<AuthenticationProviderConfiguration> Providers { get; set; } = [];
}
