namespace mark.davison.kyiv.api;

[UseCQRSServer]
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
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            })
            .AddLogging()
            .AddSingleton<IDataSeeder, KyivDataSeeder>()
            .AddAuthorization()
            .AddJwtAuthentication<KyivDbContext>(AppSettings.AUTHENTICATION)
            .AddDatabase<KyivDbContext>(
                AppSettings.PRODUCTION_MODE,
                AppSettings.DATABASE,
                typeof(SqliteContextFactory))
            .AddCoreDbContext<KyivDbContext>()
            .AddHealthCheckServices<ApplicationHealthStateHostedService>()
            .AddServerCore()
            .AddCQRSServer();
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
                    .MapBackendRemoteAuthenticationEndpoints<KyivDbContext>()
                    .MapCQRSEndpoints()
                    .MapCommonHealthChecks();
            });
    }

}
