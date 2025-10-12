namespace mark.davison.kyiv.shared.models.Entities;

public class ExternalLogin : KyivEntity
{
    public required string Provider { get; set; }           // e.g. "Google", "GitHub", "Keycloak"
    public required string ProviderSubject { get; set; }    // e.g. OIDC sub or OAuth user ID
}
