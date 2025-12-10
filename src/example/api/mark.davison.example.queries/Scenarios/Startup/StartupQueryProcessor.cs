namespace mark.davison.example.queries.Scenarios.Startup;

public sealed class StartupQueryProcessor : IQueryProcessor<StartupQueryRequest, StartupQueryResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IOptions<AuthenticationSettings> _authSettings;

    public StartupQueryProcessor(
        IDbContext dbContext,
        IOptions<AuthenticationSettings> authSettings)
    {
        _dbContext = dbContext;
        _authSettings = authSettings;
    }

    public async Task<StartupQueryResponse> ProcessAsync(StartupQueryRequest request, ICurrentUserContext currentUserContext, CancellationToken cancellationToken)
    {
        var providers = _authSettings.Value.Providers.Select(_ => _.Name).ToList();

        var roles = await _dbContext.Set<UserRole>().AsNoTracking().Select(_ => _.Role!.Name).ToListAsync(cancellationToken);

        return new StartupQueryResponse
        {
            Value = new StartupQueryDto(providers, roles)
        };
    }
}
