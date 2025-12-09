namespace mark.davison.kyiv.web.components.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddKyivComponents(this IServiceCollection services)
    {
        services.UseClientRepository(WebConstants.ApiClientName, WebConstants.LocalBffRoot);
        services.UseAuthentication(WebConstants.ApiClientName);
        services.UseClientCQRS(typeof(Routes));
        services.UseCommonClient();

        return services;
    }
}
