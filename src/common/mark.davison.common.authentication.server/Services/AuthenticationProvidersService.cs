using mark.davison.common.authentication.server.Configuration;
using mark.davison.common.server.abstractions.Services;
using Microsoft.Extensions.Options;

namespace mark.davison.common.authentication.server.Services;

internal sealed class AuthenticationProvidersService : IAuthenticationProvidersService
{
    private readonly IOptions<AuthenticationSettings> _authenticationSettings;

    public AuthenticationProvidersService(IOptions<AuthenticationSettings> authenticationSettings)
    {
        _authenticationSettings = authenticationSettings;
    }

    public IReadOnlyList<string> GetConfiguredProviders()
    {
        return [.. _authenticationSettings.Value.Providers.Select(_ => _.Name)];
    }
}
