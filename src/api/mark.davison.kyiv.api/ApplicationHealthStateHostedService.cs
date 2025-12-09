namespace mark.davison.kyiv.api;

public class ApplicationHealthStateHostedService : ApiApplicationHealthStateHostedService<KyivDbContext, AppSettings>
{
    public ApplicationHealthStateHostedService(
        IApplicationHealthState applicationHealthState,
        IHostApplicationLifetime hostApplicationLifetime,
        IDbContextFactory<KyivDbContext> dbContextFactory,
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

    protected override async Task InitDatabaseProduction(KyivDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
