namespace mark.davison.kyiv.api;

public sealed class Startup
{
    public IConfiguration Configuration { get; }

    public AppSettings AppSettings { get; } = new();

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        IConfigurationSection section = Configuration.GetSection(AppSettings.SECTION);
        services.Configure<AppSettings>(section);
        section.Bind(AppSettings);

        services
            .AddCors()
            .AddLogging()
            .AddSingleton<IDateService>(_ => new DateService(DateService.DateMode.Utc))
            .AddSingleton<IDataSeeder, KyivDataSeeder>()
            .AddRedis(AppSettings.REDIS, "zeno_kyiv_dev_")
            .AddServerAuthentication<KyivDbContext>(AppSettings.AUTHENTICATION)
            .AddHealthChecks();

        services
            .AddHttpClient()
            .AddHttpContextAccessor()
            .AddDbContextFactory<KyivDbContext>(_ =>
            {
                _.UseSqlite($"Data Source=Kyiv.db");
                _.EnableSensitiveDataLogging();
                _.EnableDetailedErrors();
            })
            .AddHostedService<ApplicationHealthStateHostedService>();
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
                    .MapAuthentication<KyivDbContext>();
            });
    }

}
