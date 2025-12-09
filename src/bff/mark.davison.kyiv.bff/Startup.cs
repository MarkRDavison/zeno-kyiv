namespace mark.davison.kyiv.bff;

public sealed class Startup
{
    public IConfiguration Configuration { get; }

    public AppSettings AppSettings { get; set; } = new();

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    public void ConfigureServices(IServiceCollection services)
    {
        AppSettings = services.BindAppSettings(Configuration);

        services
            .AddCors(o =>
            {
                o.AddDefaultPolicy(builder =>
                {
                    builder
                        .WithOrigins(AppSettings.WEB_ORIGIN)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            })
            .AddLogging()
            .AddServerCore()
            .AddScoped<IUserAuthenticationService, RemoteUserAuthenticationService>()
            .AddRedis(AppSettings.REDIS, "zeno_kyiv_dev_")
            .AddRemoteForwarderAuthentication(AppSettings.API_ENDPOINT)
            .AddOidcCookieAuthentication(
                AppSettings.AUTHENTICATION,
                (s, email, name) =>
                {
                    var now = s.GetRequiredService<IDateService>().Now;
                    return new UserDto(Guid.NewGuid(), TenantIds.SystemTenantId, email, name, true, now, now);
                })
            .AddHealthChecks();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {

        app
            .UseHttpsRedirection()
            .UseRouting()
            .UseCors()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints
                    .MapInteractiveAuthenticationEndpoints(AppSettings.WEB_ORIGIN)
                    .UseApiProxy(AppSettings.API_ENDPOINT);
            });
    }
}
