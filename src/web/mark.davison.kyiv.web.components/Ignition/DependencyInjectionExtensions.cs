namespace mark.davison.kyiv.web.components.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddKyivComponents(this IServiceCollection services)
    {
        services
            .AddAuthentication()
            .AddSingleton<IClientHttpRepository>(_ =>
            {
                return new ClientHttpRepository(
                    "https://localhost:40000",
                    _.GetRequiredService<IHttpClientFactory>().CreateClient("API"),
                    _.GetRequiredService<ILogger<ClientHttpRepository>>());
            })
            .AddHttpClient("API")
            .AddHttpMessageHandler(_ => new CookieHandler());

        return services;
    }
}
