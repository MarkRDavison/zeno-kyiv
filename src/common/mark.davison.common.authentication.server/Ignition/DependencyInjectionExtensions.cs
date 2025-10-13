namespace mark.davison.common.authentication.server.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddServerAuthentication(this IServiceCollection services)
    {

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        services
            .AddMemoryCache();

        services
            .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IRedisTicketStore, IDateService>(
                (options, store, dateService) =>
                {
                    options.SessionStore = store;
                    options.SlidingExpiration = true;
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        if (await AuthTokenHelpers.RefreshTokenIfNeeded(dateService, store, context.Properties))
                        {
                            context.ShouldRenew = true;
                        }
                    };
                });

        services
            .AddSingleton<IRedisTicketStore, RedisTicketStore>();

        return services;
    }

}
