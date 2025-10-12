namespace mark.davison.kyiv.api.Configuration;

public class AuthenticationProviderConfiguration
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "oidc";
    public string? Authority { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? AuthorizationEndpoint { get; init; }
    public string? TokenEndpoint { get; init; }
    public string? UserInformationEndpoint { get; init; }
    public string? EndSessionEndpoint { get; init; }
    public string[]? Scope { get; init; }
}
