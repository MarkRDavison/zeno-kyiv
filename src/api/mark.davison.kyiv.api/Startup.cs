using mark.davison.common.CQRS;
using mark.davison.common.server.abstractions.CQRS;
using mark.davison.common.server.CQRS;
using mark.davison.common.server.Ignition;
using mark.davison.common.source.generators.CQRS;
using mark.davison.kyiv.api.queries;
using mark.davison.kyiv.api.queries.Scenarios.AdminSettings;
using mark.davison.kyiv.api.queries.Scenarios.Startup;
using mark.davison.kyiv.shared.models.dto;
using mark.davison.kyiv.shared.models.dto.Scenarios.Queries.AdminSettings;
using mark.davison.kyiv.shared.models.dto.Scenarios.Queries.Startup;

namespace mark.davison.kyiv.api;

[UseCQRSServer(typeof(Startup), typeof(DtosRootType)/*, typeof(CommandsRootType)*/, typeof(QueriesRootType))]
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
            .AddSingleton<IDataSeeder, KyivDataSeeder>()
            .AddAuthorization()
            .AddJwtAuthentication<KyivDbContext>(AppSettings.AUTHENTICATION)
            .AddHttpClient()
            .AddHttpContextAccessor()
            .AddDbContextFactory<KyivDbContext>(_ =>
            {
                _.UseSqlite($"Data Source=Kyiv.db");
                _.EnableSensitiveDataLogging();
                _.EnableDetailedErrors();
            })
            .AddHostedService<ApplicationHealthStateHostedService>()
            .AddServerCore()
            // TODO: SO MANUAL
            .AddScoped<IQueryProcessor<StartupQueryRequest, StartupQueryResponse>, StartupQueryProcessor>()
            .AddScoped<IQueryHandler<StartupQueryRequest, StartupQueryResponse>>(_ =>
            {
                return new ValidateAndProcessQueryHandler<StartupQueryRequest, StartupQueryResponse>(
                    _.GetRequiredService<IQueryProcessor<StartupQueryRequest, StartupQueryResponse>>());
            })
            .AddScoped<IQueryProcessor<AdminSettingsQueryRequest, AdminSettingsQueryResponse>, AdminSettingsQueryProcessor>()
            .AddScoped<IQueryHandler<AdminSettingsQueryRequest, AdminSettingsQueryResponse>>(_ =>
            {
                return new ValidateAndProcessQueryHandler<AdminSettingsQueryRequest, AdminSettingsQueryResponse>(
                    _.GetRequiredService<IQueryProcessor<AdminSettingsQueryRequest, AdminSettingsQueryResponse>>());
            });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors(builder =>
            builder
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
                endpoints
                    .MapBackendRemoteAuthenticationEndpoints<KyivDbContext>();
                // TODO: SO MANUAL
                endpoints
                    .MapGet(
                        "/api/startup-query",
                        async (HttpContext context, CancellationToken cancellationToken) =>
                        {
                            var dispatcher = context.RequestServices.GetRequiredService<IQueryDispatcher>();
                            return await dispatcher.Dispatch<StartupQueryRequest, StartupQueryResponse>(cancellationToken);
                        })
                    .AllowAnonymous();

                endpoints
                    .MapGet(
                        "/api/admin-settings",
                        async (HttpContext context, CancellationToken cancellationToken) =>
                        {
                            var dispatcher = context.RequestServices.GetRequiredService<IQueryDispatcher>();
                            return await dispatcher.Dispatch<AdminSettingsQueryRequest, AdminSettingsQueryResponse>(cancellationToken);
                        })
                    .RequireAuthorization(p => p.RequireRole("Admin"));
            });
    }

}
