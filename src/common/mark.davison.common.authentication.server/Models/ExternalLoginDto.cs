namespace mark.davison.common.authentication.server.Models;

public record ExternalLoginDto(Guid Id, Guid UserId, string Provider, string ProviderSub);