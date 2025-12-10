namespace mark.davison.example.web.components.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddExampleComponents(this IServiceCollection services)
    {
        services.UseClientRepository(WebConstants.ApiClientName, WebConstants.LocalBffRoot);
        services.UseAuthentication(WebConstants.ApiClientName);
        services.UseClientCQRS(typeof(Routes));
        services.UseCommonClient();

        return services;
    }
}
