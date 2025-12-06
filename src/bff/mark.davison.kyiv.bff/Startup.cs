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
            .AddCors()
            .AddLogging()
            .AddSingleton<IDateService>(_ => new DateService(DateService.DateMode.Utc))
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
        app.UseCors(b =>
            b
                .SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader());
        app
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapInteractiveAuthenticationEndpoints();
            });
    }
}
