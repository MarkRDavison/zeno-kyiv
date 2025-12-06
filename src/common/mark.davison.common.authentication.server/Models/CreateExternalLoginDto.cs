namespace mark.davison.common.authentication.server.Models;

public record CreateExternalLoginDto(string Provider, string ProviderSub);
