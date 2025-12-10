namespace mark.davison.example.api;

public class ApplicationHealthStateHostedService : ApiApplicationHealthStateHostedService<ExampleDbContext, AppSettings>
{
    public ApplicationHealthStateHostedService(
        IApplicationHealthState applicationHealthState,
        IHostApplicationLifetime hostApplicationLifetime,
        IDbContextFactory<ExampleDbContext> dbContextFactory,
        IOptions<AppSettings> appSettings,
        IDataSeeder? dataSeeder
    ) : base(
        applicationHealthState,
        hostApplicationLifetime,
        dbContextFactory,
        appSettings,
        dataSeeder)
    {
    }

    protected override async Task InitDatabaseProduction(ExampleDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}