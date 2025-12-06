namespace mark.davison.kyiv.web.components.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddKyivComponents(this IServiceCollection services)
    {
        services
            .AddAuthentication()
            .AddHttpClient("API")
            .AddHttpMessageHandler(_ => new CookieHandler());

        return services;
    }
}
