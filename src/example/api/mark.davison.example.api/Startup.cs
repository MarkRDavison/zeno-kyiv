using mark.davison.common.server.Middleware;

namespace mark.davison.example.api;

[UseCQRSServer]
public class Startup(IConfiguration Configuration)
{
    public AppSettings AppSettings { get; set; } = new();

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
            .AddSingleton<IDataSeeder, ExampleDataSeeder>()
            .AddAuthorization()
            .AddJwtAuthentication<ExampleDbContext>(AppSettings.AUTHENTICATION)
            .AddDatabase<ExampleDbContext>(
                AppSettings.PRODUCTION_MODE,
                AppSettings.DATABASE,
                typeof(SqliteContextFactory))
            .AddCoreDbContext<ExampleDbContext>()
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
            .UseMiddleware<RequestResponseLoggingMiddleware>()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints
                    .MapBackendRemoteAuthenticationEndpoints<ExampleDbContext>()
                    .MapCQRSEndpoints()
                    .MapCommonHealthChecks();
            });
    }
}