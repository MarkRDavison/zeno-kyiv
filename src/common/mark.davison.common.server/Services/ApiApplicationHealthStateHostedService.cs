namespace mark.davison.common.server.Services;

public abstract class ApiApplicationHealthStateHostedService<TDbContext, TAppSettings> : IHostedService
    where TDbContext : DbContext
    where TAppSettings : class, IRootAppSettings
{
    protected readonly IApplicationHealthState _applicationHealthState;
    protected readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;
    private readonly IOptions<TAppSettings> _appSettings;
    private readonly IDataSeeder? _dataSeeder;

    public ApiApplicationHealthStateHostedService(
        IApplicationHealthState applicationHealthState,
        IHostApplicationLifetime hostApplicationLifetime,
        IDbContextFactory<TDbContext> dbContextFactory,
        IOptions<TAppSettings> appSettings,
        IDataSeeder? dataSeeder)
    {
        _applicationHealthState = applicationHealthState;
        _hostApplicationLifetime = hostApplicationLifetime;
        _dbContextFactory = dbContextFactory;
        _appSettings = appSettings;
        _dataSeeder = dataSeeder;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        _hostApplicationLifetime.ApplicationStarted.Register(() =>
        {
            _applicationHealthState.Started = true;
        });

        _hostApplicationLifetime.ApplicationStopping.Register(() =>
        {
            _applicationHealthState.Ready = false;
        });

        _hostApplicationLifetime.ApplicationStopped.Register(() =>
        {
            _applicationHealthState.Ready = false;
        });

        _ = BaseStartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        _applicationHealthState.Ready = false;
    }

    protected abstract Task InitDatabaseProduction(TDbContext dbContext, CancellationToken cancellationToken);

    protected virtual async Task InitDatabaseDevelopment(TDbContext dbContext, CancellationToken cancellationToken)
    {
        Console.WriteLine("TODO: NOCHECKIN: just for now");
        // await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    private async Task BaseStartAsync(CancellationToken cancellationToken)
    {
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            if (_appSettings.Value.PRODUCTION_MODE)
            {
                await InitDatabaseProduction(dbContext, cancellationToken);
            }
            else
            {
                await InitDatabaseDevelopment(dbContext, cancellationToken);
            }

            if (_dataSeeder is not null)
            {
                await _dataSeeder.SeedDataAsync(cancellationToken);
            }
        }

        await AdditionalStartAsync(cancellationToken);

        _applicationHealthState.Ready = true;
        _applicationHealthState.ReadySource.SetResult();
    }

    protected virtual async Task AdditionalStartAsync(CancellationToken cancellationToken)
    {
    }
}
