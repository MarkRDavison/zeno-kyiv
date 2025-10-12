namespace mark.davison.kyiv.api;

public class ApplicationHealthStateHostedService : IHostedService
{
    private bool? _started { get; set; }
    private bool? _ready { get; set; }
    private bool? _healthy { get; set; }
    private readonly TaskCompletionSource _readySource = new();

    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IDbContextFactory<KyivDbContext> _dbContextFactory;
    private readonly IOptions<AppSettings> _appSettings;

    public ApplicationHealthStateHostedService(
        IHostApplicationLifetime hostApplicationLifetime,
        IDbContextFactory<KyivDbContext> dbContextFactory,
        IOptions<AppSettings> appSettings)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _dbContextFactory = dbContextFactory;
        _appSettings = appSettings;
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

        _ = AdditionalStartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        _ready = false;
    }

    protected virtual async Task AdditionalStartAsync(CancellationToken cancellationToken)
    {
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            if (_appSettings.Value.PRODUCTION_MODE)
            {
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                await dbContext.Database.EnsureDeletedAsync(cancellationToken);
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            }
        }

        _ready = true;
        _readySource.SetResult();
    }
}
