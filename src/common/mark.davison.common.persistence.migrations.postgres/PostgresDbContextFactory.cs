namespace mark.davison.common.persistence.migrations.postgres;

public abstract class PostgresDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    protected abstract string ConfigName { get; }
    protected abstract TDbContext DbContextCreation(DbContextOptions<TDbContext> options);

    public TDbContext CreateDbContext(string[] args)
    {
        return DbContextCreation(CreateDbContextOptions());
    }

    public virtual DbConnectionInfo LoadDbConnectionInfo(IConfigurationSection config)
    {
        var info = new DbConnectionInfo
        {
            HOST = config[nameof(DbConnectionInfo.HOST)] ?? string.Empty,
            DATABASE = config[nameof(DbConnectionInfo.DATABASE)] ?? string.Empty,
            USERNAME = config[nameof(DbConnectionInfo.USERNAME)] ?? string.Empty,
            PASSWORD = config[nameof(DbConnectionInfo.PASSWORD)] ?? string.Empty,
        };

        if (int.TryParse(nameof(DbConnectionInfo.PORT), out int port))
        {
            info.PORT = port;
        }

        return info;
    }

    public DbContextOptions<TDbContext> CreateDbContextOptions()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.Development.json", true)
            .Build();

        var config = LoadDbConnectionInfo(configuration.GetSection(ConfigName));

        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();

        var conn = new NpgsqlConnectionStringBuilder
        {
            Host = config.HOST,
            Database = config.DATABASE,
            Port = config.PORT,
            Username = config.USERNAME,
            Password = config.PASSWORD
        };

        optionsBuilder.UseNpgsql(
            conn.ConnectionString,
            _ => _.MigrationsAssembly(GetType().Assembly.FullName));

        return optionsBuilder.Options;
    }
}
