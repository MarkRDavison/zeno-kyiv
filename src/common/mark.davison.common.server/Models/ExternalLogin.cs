namespace mark.davison.common.server.Models;

public class ExternalLogin : BaseEntity
{
    public required string Provider { get; set; }
    public required string ProviderSubject { get; set; }
}
