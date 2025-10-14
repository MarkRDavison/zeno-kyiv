namespace mark.davison.common.authentication.server.Configuration;

public sealed class AuthenticationSettings
{
    public string? ADMIN_EMAIL { get; set; }
    public List<AuthenticationProviderConfiguration> Providers { get; set; } = [];
}
