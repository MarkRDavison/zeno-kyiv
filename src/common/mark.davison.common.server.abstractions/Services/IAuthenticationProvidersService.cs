namespace mark.davison.common.server.abstractions.Services;

public interface IAuthenticationProvidersService
{
    IReadOnlyList<string> GetConfiguredProviders();
}
