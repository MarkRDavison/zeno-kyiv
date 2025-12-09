using mark.davison.common.authentication.server.Configuration;
using mark.davison.common.authentication.server.Ignition;

namespace mark.davison.common.server.sample.api;

[UseCQRSServer]
public class Startup
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

        var dbSettings = new DatabaseAppSettings
        {
            DATABASE_TYPE = DatabaseType.Sqlite,
            CONNECTION_STRING = "RANDOM"
        };

        var authSettings = new AuthenticationSettings
        {

        };

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
            .AddSingleton<IDataSeeder, CoreDataSeeder>()
            .AddHealthCheckServices<ApplicationHealthStateHostedService>()
            .AddJwtAuthentication<TestDbContext>(authSettings)
            .AddCQRSServer()
            .AddDatabase<TestDbContext>(
                AppSettings.PRODUCTION_MODE,
                dbSettings)
            .AddCoreDbContext<TestDbContext>();
    }

    public void Configure(IApplicationBuilder app)
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
                    .MapCQRSEndpoints()
                    .MapCommonHealthChecks();
            });
    }
}
