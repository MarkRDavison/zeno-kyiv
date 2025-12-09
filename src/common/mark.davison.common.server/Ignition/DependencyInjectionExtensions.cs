namespace mark.davison.common.server.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddServerCore(this IServiceCollection services)
    {
        return services
            .AddHttpContextAccessor()
            .AddSingleton<IDateService>(_ => new DateService(DateService.DateMode.Utc));
    }

    public static IServiceCollection AddRedis(this IServiceCollection services, RedisSettings settings, string instanceName)
    {
        var config = new ConfigurationOptions
        {
            EndPoints = { settings.HOST + ":" + settings.PORT },
            Password = settings.PASSWORD
        };
        var redis = ConnectionMultiplexer.Connect(config);
        services.AddStackExchangeRedisCache(_ =>
        {
            _.InstanceName = instanceName;
            _.Configuration = redis.Configuration;
        });

        return services;
    }

    public static IServiceCollection AddHealthCheckServices<THealthHosted>(this IServiceCollection services)
        where THealthHosted : class, IHostedService
    {
        services.AddSingleton<IApplicationHealthState, ApplicationHealthState>();

        services.AddHealthChecks()
            .AddCheck<StartupHealthCheck>(StartupHealthCheck.Name)
            .AddCheck<LiveHealthCheck>(LiveHealthCheck.Name)
            .AddCheck<ReadyHealthCheck>(ReadyHealthCheck.Name);

        services.AddHostedService<THealthHosted>();

        return services;
    }

    public static IServiceCollection AddCronJob<T>(this IServiceCollection services, Action<IScheduleConfig<T>> options) where T : CronJobService
    {
        var config = new ScheduleConfig<T>();
        options.Invoke(config);
        if (string.IsNullOrWhiteSpace(config.CronExpression))
        {
            throw new ArgumentNullException(nameof(ScheduleConfig<T>.CronExpression), @"Empty Cron Expression is not allowed.");
        }

        services.AddSingleton<IScheduleConfig<T>>(config);
        services.AddHostedService<T>();
        return services;
    }
}
