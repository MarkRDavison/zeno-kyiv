namespace mark.davison.common.client.web.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAuthentication(this IServiceCollection services)
    {
        services
            .AddAuthorizationCore()
            .AddCascadingAuthenticationState()
            .AddSingleton<AuthenticationStateProvider, CommonAuthenticationStateProvider>()
            .AddSingleton<IClientNavigationManager, ClientNavigationManager>()
            .AddSingleton<IAuthenticationService, AuthenticationService>()
            .AddMudServices();

        return services;
    }

}
