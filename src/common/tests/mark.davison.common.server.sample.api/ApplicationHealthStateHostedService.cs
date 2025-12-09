using mark.davison.common.server.abstractions.Health;
using mark.davison.common.server.sample.api.Configuration;
using mark.davison.common.server.sample.api.Persistence;
using mark.davison.common.server.Services;
using Microsoft.Extensions.Options;

namespace mark.davison.common.server.sample.api;

public class ApplicationHealthStateHostedService : ApiApplicationHealthStateHostedService<TestDbContext, AppSettings>
{
    public ApplicationHealthStateHostedService(IApplicationHealthState applicationHealthState, IHostApplicationLifetime hostApplicationLifetime, IDbContextFactory<TestDbContext> dbContextFactory, IOptions<AppSettings> appSettings, IDataSeeder? dataSeeder) : base(applicationHealthState, hostApplicationLifetime, dbContextFactory, appSettings, dataSeeder)
    {
    }

    protected override async Task InitDatabaseProduction(TestDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
