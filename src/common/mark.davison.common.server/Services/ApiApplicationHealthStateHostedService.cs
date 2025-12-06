using mark.davison.common.persistence.abstractions.Helpers;
using mark.davison.common.server.abstractions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace mark.davison.common.server.Services;

public abstract class ApiApplicationHealthStateHostedService<TDbContext, TAppSettings> : IHostedService
    where TDbContext : DbContext
    where TAppSettings : class, IRootAppSettings
{
    private bool? _started { get; set; }
    private bool? _ready { get; set; }
    private bool? _healthy { get; set; }
    private readonly TaskCompletionSource _readySource = new();

    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;
    private readonly IOptions<TAppSettings> _appSettings;
    private readonly IDataSeeder? _dataSeeder;

    public ApiApplicationHealthStateHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        IDbContextFactory<TDbContext> dbContextFactory,
        IOptions<TAppSettings> appSettings,
        IDataSeeder? dataSeeder)
    {
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
            _started = true;
        });

        _hostApplicationLifetime.ApplicationStopping.Register(() =>
        {
            _ready = false;
        });

        _hostApplicationLifetime.ApplicationStopped.Register(() =>
        {
            _ready = false;
        });

        _ = BaseStartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        _ready = false;
    }

    protected abstract Task InitDatabaseProduction(TDbContext dbContext, CancellationToken cancellationToken);

    protected virtual async Task InitDatabaseDevelopment(TDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
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

        _ready = true;
        _readySource.SetResult();
    }

    protected virtual async Task AdditionalStartAsync(CancellationToken cancellationToken)
    {
    }
}
