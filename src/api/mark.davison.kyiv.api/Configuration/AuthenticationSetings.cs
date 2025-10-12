namespace mark.davison.kyiv.api.Configuration;

public class AuthenticationSetings
{
    public string? DefaultScheme { get; set; }
    public string? DefaultChallengeScheme { get; set; }
    public List<AuthenticationProviderConfiguration> Providers { get; set; } = [];
}
