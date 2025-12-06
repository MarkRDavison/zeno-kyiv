using mark.davison.common.server.CQRS;

namespace mark.davison.common.server.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddServerCore(this IServiceCollection services)
    {
        return services
            .AddScoped<IQueryDispatcher, QueryDispatcher>();
    }
    public static IServiceCollection AddRedis(this IServiceCollection services, RedisSettings settings, string instanceName)
    {
        var config = new ConfigurationOptions
        {
            EndPoints = { settings.HOST + ":" + settings.PORT },
            Password = settings.PASSWORD
        };
        IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(config);
        services.AddStackExchangeRedisCache(_ =>
        {
            _.InstanceName = instanceName;
            _.Configuration = redis.Configuration;
        });

        return services;
    }
}
